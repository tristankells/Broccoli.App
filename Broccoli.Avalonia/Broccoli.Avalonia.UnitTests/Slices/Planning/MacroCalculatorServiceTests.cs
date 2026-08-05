using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;

namespace Broccoli.Avalonia.Tests.Slices.Planning;

[TestClass]
public class MacroCalculatorServiceTests
{
    private readonly MacroCalculatorService _calculator = new();

    private static MacroTargetSettings MetricSettings() => new()
    {
        UnitSystem = UnitSystem.Metric,
        BmrFormula = BmrFormula.MifflinStJeor,
        ProteinMethod = ProteinMethod.RatioPercent,
        ProteinPercent = 30,
        CarbPercent = 40,
        FatPercent = 30
    };

    [TestMethod]
    public void Calculate_MifflinStJeor_Male()
    {
        var target = new MacroTarget { Gender = GenderType.Male, WeightKg = 80, HeightCm = 180, Age = 30 };
        _calculator.Calculate(target, MetricSettings());

        Assert.AreEqual(1780, target.Bmr);
        Assert.IsTrue(target.Tdee > target.Bmr);
    }

    [TestMethod]
    public void Calculate_MifflinStJeor_Female()
    {
        var target = new MacroTarget { Gender = GenderType.Female, WeightKg = 65, HeightCm = 165, Age = 25 };
        _calculator.Calculate(target, MetricSettings());

        Assert.AreEqual(1396, target.Bmr);
    }

    [TestMethod]
    public void Calculate_MifflinStJeor_Other_AveragesMaleAndFemale()
    {
        var target = new MacroTarget { Gender = GenderType.Other, WeightKg = 80, HeightCm = 180, Age = 30 };
        _calculator.Calculate(target, MetricSettings());

        var maleTarget = new MacroTarget { Gender = GenderType.Male, WeightKg = 80, HeightCm = 180, Age = 30 };
        _calculator.Calculate(maleTarget, MetricSettings());

        Assert.IsTrue(target.Bmr > 0);
        Assert.AreNotEqual(maleTarget.Bmr, target.Bmr);
    }

    [TestMethod]
    public void Calculate_HarrisBenedict_Male()
    {
        MacroTargetSettings settings = MetricSettings();
        settings.BmrFormula = BmrFormula.HarrisBenedict;
        var target = new MacroTarget { Gender = GenderType.Male, WeightKg = 80, HeightCm = 180, Age = 30 };

        _calculator.Calculate(target, settings);

        Assert.AreEqual(1854, target.Bmr);
    }

    [TestMethod]
    public void Calculate_HarrisBenedict_Female()
    {
        MacroTargetSettings settings = MetricSettings();
        settings.BmrFormula = BmrFormula.HarrisBenedict;
        var target = new MacroTarget { Gender = GenderType.Female, WeightKg = 65, HeightCm = 165, Age = 25 };

        _calculator.Calculate(target, settings);

        Assert.AreEqual(1452, target.Bmr);
    }

    [TestMethod]
    public void Calculate_ZeroInputs_ReturnsZeroBmr()
    {
        var target = new MacroTarget { WeightKg = 0, HeightCm = 0, Age = 0 };
        _calculator.Calculate(target, MetricSettings());

        Assert.AreEqual(0, target.Bmr);
        Assert.AreEqual(0, target.Tdee);
    }

    [TestMethod]
    public void Calculate_ActivityLevel_ScalesTdee()
    {
        MacroTargetSettings settings = MetricSettings();
        var sed = new MacroTarget { ActivityLevel = ActivityLevel.Sedentary, WeightKg = 80, HeightCm = 180, Age = 30 };
        var vAct = new MacroTarget { ActivityLevel = ActivityLevel.VeryActive, WeightKg = 80, HeightCm = 180, Age = 30 };

        _calculator.Calculate(sed, settings);
        _calculator.Calculate(vAct, settings);

        Assert.AreEqual(sed.Bmr, vAct.Bmr);
        Assert.IsTrue(vAct.Tdee > sed.Tdee);
    }

    [TestMethod]
    public void Calculate_ExtraActive_HasHighestTdee()
    {
        MacroTargetSettings settings = MetricSettings();
        var extra = new MacroTarget { ActivityLevel = ActivityLevel.ExtraActive, WeightKg = 80, HeightCm = 180, Age = 30 };
        _calculator.Calculate(extra, settings);

        Assert.AreEqual(Math.Ceiling(extra.Bmr * 1.900), extra.Tdee);
    }

    [TestMethod]
    public void Calculate_GoalCalorieDelta_AdjustsRecommendedCalories()
    {
        MacroTargetSettings settings = MetricSettings();
        var target = new MacroTarget { WeightKg = 80, HeightCm = 180, Age = 30, GoalCalorieDelta = -500 };
        _calculator.Calculate(target, settings);

        Assert.AreEqual(Math.Ceiling(target.Tdee - 500), target.RecommendedCalories);

        target.GoalCalorieDelta = 300;
        _calculator.Calculate(target, settings);

        Assert.AreEqual(Math.Ceiling(target.Tdee + 300), target.RecommendedCalories);
    }

    [TestMethod]
    public void Calculate_RecommendedCalories_FloorAtZero()
    {
        MacroTargetSettings settings = MetricSettings();
        var target = new MacroTarget { WeightKg = 40, HeightCm = 100, Age = 100, ActivityLevel = ActivityLevel.Sedentary, GoalCalorieDelta = -10000 };
        _calculator.Calculate(target, settings);

        Assert.AreEqual(0, target.RecommendedCalories);
    }

    [TestMethod]
    public void Calculate_Imperial_ConvertsBeforeCalculation()
    {
        var settings = new MacroTargetSettings
        {
            UnitSystem = UnitSystem.Imperial,
            BmrFormula = BmrFormula.MifflinStJeor,
            ProteinMethod = ProteinMethod.RatioPercent,
            ProteinPercent = 30,
            CarbPercent = 40,
            FatPercent = 30
        };
        var metricTarget = new MacroTarget { Gender = GenderType.Male, WeightKg = 80, HeightCm = 180, Age = 30 };
        var imperialTarget = new MacroTarget
        {
            Gender = GenderType.Male,
            WeightKg = 80 * 2.20462,   // 80 kg in lbs
            HeightCm = 180 / 2.54,      // 180 cm in inches
            Age = 30
        };

        _calculator.Calculate(metricTarget, MetricSettings());
        _calculator.Calculate(imperialTarget, settings);

        Assert.AreEqual(metricTarget.Bmr, imperialTarget.Bmr);
    }

    [TestMethod]
    public void Calculate_ProteinMethod_RatioPercent_AllocatesByPercentage()
    {
        MacroTargetSettings settings = MetricSettings();
        var target = new MacroTarget { WeightKg = 80, HeightCm = 180, Age = 30 };
        _calculator.Calculate(target, settings);

        Assert.IsTrue(target.RecommendedProteinG > 0);
        Assert.IsTrue(target.RecommendedCarbsG > 0);
        Assert.IsTrue(target.RecommendedFatG > 0);
    }

    [TestMethod]
    public void Calculate_ProteinMethod_GramsPerKg_ProteinFixedByWeight()
    {
        var settings = new MacroTargetSettings
        {
            UnitSystem = UnitSystem.Metric,
            BmrFormula = BmrFormula.MifflinStJeor,
            ProteinMethod = ProteinMethod.GramsPerKg,
            ProteinGramsPerKg = 2.0,
            CarbPercent = 60,
            FatPercent = 40,
        };
        var target = new MacroTarget { WeightKg = 80, HeightCm = 180, Age = 30 };
        _calculator.Calculate(target, settings);

        Assert.AreEqual(160, target.RecommendedProteinG);
    }

    [TestMethod]
    public void Calculate_ProteinMethod_GramsPerKg_ExceedsCalories_ZeroCarbsFat()
    {
        var settings = new MacroTargetSettings
        {
            UnitSystem = UnitSystem.Metric,
            BmrFormula = BmrFormula.MifflinStJeor,
            ProteinMethod = ProteinMethod.GramsPerKg,
            ProteinGramsPerKg = 2.2,
            CarbPercent = 60,
            FatPercent = 40,
        };
        var target = new MacroTarget { WeightKg = 80, HeightCm = 180, Age = 30, ActivityLevel = ActivityLevel.Sedentary, GoalCalorieDelta = -3000 };
        _calculator.Calculate(target, settings);

        Assert.AreEqual(0, target.RecommendedCalories);
        Assert.AreEqual(0, target.RecommendedCarbsG);
        Assert.AreEqual(0, target.RecommendedFatG);
    }

    [TestMethod]
    public void WeightChangeKgPerWeek_NegativeDelta_ReturnsNegativeLoss()
    {
        double result = MacroCalculatorService.WeightChangeKgPerWeek(-500);
        Assert.IsTrue(result < 0);
        Assert.AreEqual(-0.45, result, 0.01);
    }

    [TestMethod]
    public void WeightChangeKgPerWeek_PositiveDelta_ReturnsPositiveGain()
    {
        double result = MacroCalculatorService.WeightChangeKgPerWeek(500);
        Assert.IsTrue(result > 0);
        Assert.AreEqual(0.45, result, 0.01);
    }

    [TestMethod]
    public void WeightChangeKgPerWeek_ZeroDelta_ReturnsZero()
    {
        double result = MacroCalculatorService.WeightChangeKgPerWeek(0);
        Assert.AreEqual(0, result);
    }
}
