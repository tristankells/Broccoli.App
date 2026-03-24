using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Slices.Nutrition;

/// <summary>
/// Pure stateless service that calculates BMR, TDEE and macro targets
/// using industry-standard formulas. Mutates the calculated fields on the
/// supplied <see cref="MacroTarget"/> in-place.
/// </summary>
public class MacroCalculatorService
{
    // Activity multipliers per ACSM / Mifflin guidelines
    private static readonly Dictionary<ActivityLevel, double> ActivityMultipliers = new()
    {
        { ActivityLevel.Sedentary,        1.200 },
        { ActivityLevel.LightlyActive,    1.375 },
        { ActivityLevel.ModeratelyActive, 1.550 },
        { ActivityLevel.VeryActive,       1.725 },
        { ActivityLevel.ExtraActive,      1.900 }
    };

    /// <summary>
    /// Calculates and stores BMR, TDEE, RecommendedCalories and all macro grams
    /// directly onto <paramref name="target"/>. Safe to call with zero / empty values
    /// (results will simply be 0).
    /// </summary>
    public void Calculate(MacroTarget target, MacroTargetSettings settings)
    {
        // Convert to metric internally
        var weightKg = settings.UnitSystem == UnitSystem.Imperial
            ? target.WeightKg / 2.20462
            : target.WeightKg;

        var heightCm = settings.UnitSystem == UnitSystem.Imperial
            ? target.HeightCm * 2.54
            : target.HeightCm;

        target.Bmr   = Math.Ceiling(CalculateBmr(target.Gender, weightKg, heightCm, target.Age, settings.BmrFormula));
        target.Tdee  = Math.Ceiling(target.Bmr * ActivityMultipliers[target.ActivityLevel]);

        target.RecommendedCalories = Math.Ceiling(target.Tdee + target.GoalCalorieDelta);
        if (target.RecommendedCalories < 0) target.RecommendedCalories = 0;

        CalculateMacros(target, weightKg, settings);
    }

    // -- BMR ------------------------------------------------------------------

    private static double CalculateBmr(
        GenderType gender, double weightKg, double heightCm, int age, BmrFormula formula)
    {
        if (weightKg <= 0 || heightCm <= 0 || age <= 0) return 0;

        return formula switch
        {
            BmrFormula.HarrisBenedict => CalculateHarrisBenedict(gender, weightKg, heightCm, age),
            _                          => CalculateMifflinStJeor(gender, weightKg, heightCm, age)
        };
    }

    /// <summary>Mifflin-St Jeor (1990) � most widely recommended by dietitians.</summary>
    private static double CalculateMifflinStJeor(
        GenderType gender, double weightKg, double heightCm, int age)
    {
        var male   = (10 * weightKg) + (6.25 * heightCm) - (5 * age) + 5;
        var female = (10 * weightKg) + (6.25 * heightCm) - (5 * age) - 161;

        return gender switch
        {
            GenderType.Male   => male,
            GenderType.Female => female,
            _                  => (male + female) / 2   // Other: average
        };
    }

    /// <summary>Revised Harris-Benedict (Roza &amp; Shizgal, 1984).</summary>
    private static double CalculateHarrisBenedict(
        GenderType gender, double weightKg, double heightCm, int age)
    {
        var male   = 88.362  + (13.397 * weightKg) + (4.799 * heightCm) - (5.677 * age);
        var female = 447.593 + (9.247  * weightKg) + (3.098 * heightCm) - (4.330 * age);

        return gender switch
        {
            GenderType.Male   => male,
            GenderType.Female => female,
            _                  => (male + female) / 2
        };
    }

    // -- Macros ---------------------------------------------------------------

    private static void CalculateMacros(MacroTarget target, double weightKg, MacroTargetSettings settings)
    {
        var calories = target.RecommendedCalories;

        if (settings.ProteinMethod == ProteinMethod.GramsPerKg && weightKg > 0)
        {
            // Protein fixed by bodyweight; carbs & fat share remaining calories proportionally
            var proteinG   = Math.Ceiling(weightKg * settings.ProteinGramsPerKg);
            var proteinCal = proteinG * 4;
            var remaining  = Math.Max(calories - proteinCal, 0);

            var carbRatioDivisor = settings.CarbPercent + settings.FatPercent;
            var carbRatio = carbRatioDivisor > 0 ? settings.CarbPercent / carbRatioDivisor : 0.5;
            var fatRatio  = carbRatioDivisor > 0 ? settings.FatPercent  / carbRatioDivisor : 0.5;

            target.RecommendedProteinG = proteinG;
            target.RecommendedCarbsG   = Math.Ceiling((remaining * carbRatio) / 4);
            target.RecommendedFatG     = Math.Ceiling((remaining * fatRatio)  / 9);
        }
        else
        {
            // All three macros derived from percentage of total calories
            target.RecommendedProteinG = Math.Ceiling((calories * settings.ProteinPercent / 100) / 4);
            target.RecommendedCarbsG   = Math.Ceiling((calories * settings.CarbPercent    / 100) / 4);
            target.RecommendedFatG     = Math.Ceiling((calories * settings.FatPercent     / 100) / 9);
        }
    }

    /// <summary>
    /// Returns estimated kg/week change at the given daily calorie delta.
    /// Negative = loss, positive = gain. Formula: 1 kg body fat ≈ 7700 kcal.
    /// </summary>
    public static double WeightChangeKgPerWeek(int calorieDelta) =>
        Math.Round((calorieDelta * 7.0) / 7700.0, 2);
}

