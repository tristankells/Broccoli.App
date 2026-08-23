namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// Computes ingredient adjustments that move a recipe's calories/macros toward a set of targets.
/// Two strategies are supported:
/// <list type="bullet">
/// <item>
/// <see cref="AutoBalanceStrategy.IndependentSinglePass"/> scales the leading contributor of each
/// selected macro one pass at a time, accepting that earlier fixes drift as later ones apply.
/// </item>
/// <item>
/// <see cref="AutoBalanceStrategy.LinearSolve"/> selects one distinct leading contributor per
/// selected target and solves a linear system so all selected targets are hit exactly, falling
/// back to the single-pass strategy when the system is singular or produces infeasible quantities.
/// </item>
/// </list>
/// </summary>
public static class AutoBalanceCalculator
{
    public const double MinimumGrams = 1.0;

    public const double MaxGrowthFactor = 5.0;

    private const double Epsilon = 1e-9;

    private static readonly AutoBalanceNutrient[] s_precedence =
    {
        AutoBalanceNutrient.Protein,
        AutoBalanceNutrient.Carbs,
        AutoBalanceNutrient.Fat,
        AutoBalanceNutrient.Calories,
    };

    public static AutoBalancePreview Calculate(
        IReadOnlyList<AutoBalanceIngredient> ingredients,
        AutoBalanceTargets targets,
        IReadOnlySet<AutoBalanceNutrient> selected,
        AutoBalanceStrategy strategy,
        double tolerancePercent = 0)
    {
        AutoBalanceTotals before = TotalsFor(ingredients);
        AutoBalanceIngredient[] originals = ingredients.ToArray();
        AutoBalanceIngredient[] working = ingredients.Select(Clone).ToArray();

        if (selected.Count == 0 || working.Length == 0)
        {
            return new AutoBalancePreview
            {
                Before = before,
                After = before,
                Adjustments = Array.Empty<AutoBalanceAdjustment>(),
            };
        }

        if (strategy == AutoBalanceStrategy.LinearSolve &&
            TryLinearSolve(working, originals, targets, selected, tolerancePercent, out List<AutoBalanceAdjustment> adjustments))
        {
            return new AutoBalancePreview
            {
                Before = before,
                After = TotalsFor(working),
                Adjustments = adjustments,
            };
        }

        List<AutoBalanceAdjustment> singlePass = IndependentSinglePass(working, originals, targets, selected, tolerancePercent);
        return new AutoBalancePreview
        {
            Before = before,
            After = TotalsFor(working),
            Adjustments = singlePass,
            UsedFallback = strategy == AutoBalanceStrategy.LinearSolve,
        };
    }

    private static List<AutoBalanceAdjustment> IndependentSinglePass(
        AutoBalanceIngredient[] working,
        AutoBalanceIngredient[] originals,
        AutoBalanceTargets targets,
        IReadOnlySet<AutoBalanceNutrient> selected,
        double tolerancePercent)
    {
        var adjustments = new List<AutoBalanceAdjustment>();
        AutoBalanceTotals totals = TotalsFor(working);

        foreach (AutoBalanceNutrient nutrient in s_precedence.Where(selected.Contains))
        {
            double current = Current(totals, nutrient);
            double target = Target(targets, nutrient);
            double delta = target - current;

            if (Math.Abs(delta) < 0.5 || WithinTolerance(current, target, tolerancePercent))
            {
                continue;
            }

            int pivotIndex = FindLeadingContributor(working, nutrient);
            if (pivotIndex < 0)
            {
                continue;
            }

            AutoBalanceIngredient pivot = working[pivotIndex];
            double density = pivot.Density(nutrient);
            if (density <= 0)
            {
                continue;
            }

            double afterGrams = Clamp(pivot.Grams + delta / density, originals[pivotIndex].Grams);
            if (Math.Abs(afterGrams - pivot.Grams) < 0.01)
            {
                continue;
            }

            pivot.Grams = afterGrams;
            totals = TotalsFor(working);
            adjustments.Add(new AutoBalanceAdjustment { Ingredient = originals[pivotIndex], AfterGrams = afterGrams });
        }

        return adjustments;
    }

    private static bool TryLinearSolve(
        AutoBalanceIngredient[] working,
        AutoBalanceIngredient[] originals,
        AutoBalanceTargets targets,
        IReadOnlySet<AutoBalanceNutrient> selected,
        double tolerancePercent,
        out List<AutoBalanceAdjustment> adjustments)
    {
        adjustments = new List<AutoBalanceAdjustment>();

        AutoBalanceNutrient[] ordered = s_precedence.Where(selected.Contains).ToArray();
        int n = ordered.Length;
        if (n == 0 || n > working.Length)
        {
            return false;
        }

        AutoBalanceTotals before = TotalsFor(working);
        if (ordered.All(nutrient => WithinTolerance(Current(before, nutrient), Target(targets, nutrient), tolerancePercent)))
        {
            return false;
        }

        var used = new HashSet<int>();
        var pivots = new List<int>(n);
        for (int i = 0; i < n; i++)
        {
            int pivotIndex = -1;
            double bestContribution = 0;
            for (int j = 0; j < working.Length; j++)
            {
                if (used.Contains(j) || working[j].Density(ordered[i]) <= 0)
                {
                    continue;
                }

                double contribution = working[j].Contribution(ordered[i]);
                if (contribution > bestContribution)
                {
                    bestContribution = contribution;
                    pivotIndex = j;
                }
            }

            if (pivotIndex < 0)
            {
                return false;
            }

            used.Add(pivotIndex);
            pivots.Add(pivotIndex);
        }

        double[,] a = new double[n, n];
        double[] b = new double[n];
        for (int row = 0; row < n; row++)
        {
            b[row] = Target(targets, ordered[row]) - Current(before, ordered[row]);
            for (int col = 0; col < n; col++)
            {
                a[row, col] = working[pivots[col]].Density(ordered[row]);
            }
        }

        double[]? deltas = SolveLinearSystem(a, b);
        if (deltas is null)
        {
            return false;
        }

        var newGrams = new double[n];
        for (int i = 0; i < n; i++)
        {
            double after = working[pivots[i]].Grams + deltas[i];
            if (!double.IsFinite(after) ||
                after < MinimumGrams ||
                after > working[pivots[i]].Grams * MaxGrowthFactor)
            {
                return false;
            }

            newGrams[i] = after;
        }

        for (int i = 0; i < n; i++)
        {
            int pivotIndex = pivots[i];
            working[pivotIndex].Grams = newGrams[i];
            adjustments.Add(new AutoBalanceAdjustment { Ingredient = originals[pivotIndex], AfterGrams = newGrams[i] });
        }

        return true;
    }

    private static int FindLeadingContributor(AutoBalanceIngredient[] working, AutoBalanceNutrient nutrient)
    {
        int bestIndex = -1;
        double bestContribution = 0;
        for (int i = 0; i < working.Length; i++)
        {
            if (working[i].Density(nutrient) <= 0)
            {
                continue;
            }

            double contribution = working[i].Contribution(nutrient);
            if (contribution > bestContribution)
            {
                bestContribution = contribution;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static double Clamp(double afterGrams, double originalGrams)
    {
        if (afterGrams < MinimumGrams)
        {
            return MinimumGrams;
        }

        if (afterGrams > originalGrams * MaxGrowthFactor)
        {
            return originalGrams * MaxGrowthFactor;
        }

        return afterGrams;
    }

    private static bool WithinTolerance(double current, double target, double tolerancePercent)
    {
        if (tolerancePercent <= 0 || target == 0)
        {
            return false;
        }

        return Math.Abs(current - target) / Math.Abs(target) * 100.0 <= tolerancePercent;
    }

    private static AutoBalanceTotals TotalsFor(IEnumerable<AutoBalanceIngredient> ingredients)
    {
        double cal = 0, pro = 0, carb = 0, fat = 0;
        foreach (AutoBalanceIngredient ingredient in ingredients)
        {
            cal += ingredient.Grams * ingredient.KcalPerGram;
            pro += ingredient.Grams * ingredient.ProteinPerGram;
            carb += ingredient.Grams * ingredient.CarbsPerGram;
            fat += ingredient.Grams * ingredient.FatPerGram;
        }

        return new AutoBalanceTotals { Calories = cal, ProteinG = pro, CarbsG = carb, FatG = fat };
    }

    private static double Current(AutoBalanceTotals totals, AutoBalanceNutrient nutrient) => nutrient switch
    {
        AutoBalanceNutrient.Calories => totals.Calories,
        AutoBalanceNutrient.Protein => totals.ProteinG,
        AutoBalanceNutrient.Carbs => totals.CarbsG,
        AutoBalanceNutrient.Fat => totals.FatG,
        _ => 0,
    };

    private static double Target(AutoBalanceTargets targets, AutoBalanceNutrient nutrient) => nutrient switch
    {
        AutoBalanceNutrient.Calories => targets.Calories,
        AutoBalanceNutrient.Protein => targets.ProteinG,
        AutoBalanceNutrient.Carbs => targets.CarbsG,
        AutoBalanceNutrient.Fat => targets.FatG,
        _ => 0,
    };

    private static AutoBalanceIngredient Clone(AutoBalanceIngredient source) => new()
    {
        FoodName = source.FoodName,
        FoodDescription = source.FoodDescription,
        CanonicalUnit = source.CanonicalUnit,
        Quantity = source.Quantity,
        Grams = source.Grams,
        KcalPerGram = source.KcalPerGram,
        ProteinPerGram = source.ProteinPerGram,
        CarbsPerGram = source.CarbsPerGram,
        FatPerGram = source.FatPerGram,
    };

    private static double[]? SolveLinearSystem(double[,] a, double[] b)
    {
        int n = b.Length;
        double[,] m = (double[,])a.Clone();
        double[] x = (double[])b.Clone();

        for (int col = 0; col < n; col++)
        {
            int pivot = col;
            for (int row = col + 1; row < n; row++)
            {
                if (Math.Abs(m[row, col]) > Math.Abs(m[pivot, col]))
                {
                    pivot = row;
                }
            }

            if (Math.Abs(m[pivot, col]) < Epsilon)
            {
                return null;
            }

            if (pivot != col)
            {
                for (int c = 0; c < n; c++)
                {
                    (m[col, c], m[pivot, c]) = (m[pivot, c], m[col, c]);
                }

                (x[col], x[pivot]) = (x[pivot], x[col]);
            }

            double diag = m[col, col];
            for (int row = 0; row < n; row++)
            {
                if (row == col)
                {
                    continue;
                }

                double factor = m[row, col] / diag;
                for (int c = 0; c < n; c++)
                {
                    m[row, c] -= factor * m[col, c];
                }

                x[row] -= factor * x[col];
            }
        }

        for (int i = 0; i < n; i++)
        {
            x[i] /= m[i, i];
        }

        return x;
    }
}
