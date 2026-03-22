using Broccoli.App.Shared.IngredientParsing;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.IntegrationTests;

[TestClass]
public sealed class IngredientParserServiceTests
{
    private LocalJsonFoodService _foodService;
    private ILoggerFactory _loggerFactory;
    private IngredientParserService _ingredientParserService;
    
    [TestInitialize]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole();
        });

        string foodDatabasePath = Path.Combine(AppContext.BaseDirectory, "FoodDatabase.json");
        _foodService = new LocalJsonFoodService(
            foodDatabasePath,
            _loggerFactory.CreateLogger<LocalJsonFoodService>());
        
        _ingredientParserService = new IngredientParserService(
            _foodService);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _loggerFactory.Dispose();
    }

    // Include a baseline 10 recipes with a variety of ingredient formats to ensure the parser can handle real-world data and to catch any regressions in future changes.
    [TestMethod]
    public async Task IngredientParsingBaseline()
    {
        // Temporary Comment: As you build the list of recipes, add additional unit tests to cover specific edge cases (e.g. "1 1/2 pack Malaysian curry powder" or "1 drizzle of oil") to ensure the parser can handle these correctly.
        // TODO: Make even more explicit and assert on the exact matched food item?
        // TODO: Put data into a data.csv and use ITestDataSource to make it more maintainable and easier to add more test cases in the future?
        // Arrange
        var testRecipes = new List<(string Name, string Ingredients, int ExpectedMatches, int[] ExpectedFoodIds)>
        {
            // 12 matched ingredients (water has no match)
            ("Takeaway Dupes! 20-Minute Malaysian Chicken Curry Laksa with Vermicelli Noodles",
                "250g vermicelli noodles\n1 drizzle of oil \n1 drizzle of oil \n600g carrot, grated\n1400g diced free range chicken breast\n150g Singapore laksa paste\n1 1/2 pack Malaysian curry powder\n400g lite coconut milk\n1/2 cup water \n1 twin pack baby bok choy, sliced 3cm\n1 tsp fish sauce, optional \n200g mung bean sprouts\n1 pinch of chilli flakes, optional",
                12,
                new[]
                {
                    1, // 250g vermicelli noodles
                    2, // 1 drizzle of oil
                    3, // 600g carrot, grated
                    4, // 1400g diced free range chicken breast
                    5, // 150g Singapore laksa paste
                    6, // 1 1/2 pack Malaysian curry powder
                    7, // 400g lite coconut milk
                    11, // 1/2 cup water — no match
                    8, // 1 twin pack baby bok choy, sliced 3cm
                    9, // 1 tsp fish sauce, optional
                    10, // 200g mung bean sprouts
                    12, // 1 pinch of chilli flakes, optional
                }),
            // 14 matched ingredients — Tuscan stock blend * 2 and beef stock resolve to the same food item
            ("Herby Beef Meatballs with Mash & Gravy",
                "5 potato, diced\n3 carrot, diced\n3 parsnip, diced\n1/3 cup milk \n125g sour cream\n1 brown onion, finely chopped\n2 pack Tuscan stock blend\n1500g beef mince\n1 egg \n50g panko breadcrumbs\n1 tsp salt \n1 drizzle of oil \n2 Tbsp butter \n1 pack Tuscan stock blend\n3 Tbsp flour \n1 1/2 cup beef stock",
                14,
                new[]
                {
                    13, // 5 potato, diced
                    3, // 3 carrot, diced
                    14, // 3 parsnip, diced
                    15, // 1/3 cup milk
                    16, // 125g sour cream
                    17, // 1 brown onion, finely chopped
                    18, // 2 pack Tuscan stock blend
                    19, // 1500g beef mince
                    25, // 1 egg
                    20, // 50g panko breadcrumbs
                    21, // 1 tsp salt
                    2, // 1 drizzle of oil
                    24, // 2 Tbsp butter
                    0, // 1 pack Tuscan stock blend
                    22, // 3 Tbsp flour — no match
                    23// 1 1/2 cup beef stock — no match (resolves to same food as Tuscan stock blend)
                }),
            // 16 matched ingredients
            ("Honey Miso Noodles",
                "70g honey\n35g soy sauce\n55g red miso paste\n1/3 cup chicken broth\n1 1/2 tbsp rice vinegar\n300g lo mein noodles\n2 medium onion\n400g carrots\n500g zucchini\n4 green onions\n1 tbsp minced garlic\n1400g boneless skinless chicken breast\n2 tsp garlic powder\n2 tbsp olive oil\n2 tbsp sesame seeds\nsalt and pepper to taste",
                16,
                new[]
                {
                    26, // 70g honey
                    27, // 35g soy sauce
                    28, // 55g red miso paste
                    18, // 1/3 cup chicken broth
                    30, // 1 1/2 tbsp rice vinegar
                    31, // 300g lo mein noodles
                    17, // 2 medium onion
                    3, // 400g carrots
                    32, // 500g zucchini
                    33, // 4 green onions
                    34, // 1 tbsp minced garlic
                    4, // 1400g boneless skinless chicken breast
                    35, // 2 tsp garlic powder
                    2, // 2 tbsp olive oil
                    36, // 2 tbsp sesame seeds
                      // salt and pepper to taste (should be removed)
                }),
            // 8 matched ingredients
            ("Mediterranean One Sheet Greek Chicken & Vegetables",
                "1400g boneless skinless chicken breast\n3 capsicum\n2 red onion\n1kg potato\n600g zucchini\n2 lemon\n300g feta cheese\n3 tbsp olive oil",
                8,
                new[]
                {
                    4, // 1400g boneless skinless chicken breast
                    37, // 3 capsicum
                    17, // 2 red onion
                    13, // 1kg potato
                    38, // 600g zucchini
                    39, // 2 lemon
                    40, // 300g feta cheese
                    2, // 3 tbsp olive oil
                }),
            // 14 matched ingredients
            ("20-Minute Chicken & Rice Noodle Salad with Peanut Dressing",
                "80g smooth peanut butter\n1 tsp red curry paste\n1 Tbsp soy sauce\n400ml coconut milk\n1 pack rice stick noodles\n1 telegraph cucumber, cut in half & thinly sliced on an angle\n500g slaw\n2 Tbsp vinegar\n2 Tbsp sweet chilli sauce\n1 Tbsp fish sauce\n1 drizzle of oil\n1500g diced free range chicken breast\n2 tsp red curry paste\n1 Tbsp honey\n2 Tbsp soy sauce",
                14,
                new[]
                {
                    42, // 80g smooth peanut butter
                    41, // 1 tsp red curry paste
                    27, // 1 Tbsp soy sauce
                    43, // 400ml coconut milk
                    44, // 1 pack rice stick noodles
                    45, // 1 telegraph cucumber
                    46, // 500g slaw
                    30, // 2 Tbsp vinegar
                    47, // 2 Tbsp sweet chilli sauce
                    9, // 1 Tbsp fish sauce
                    2, // 1 drizzle of oil
                    4, // 1500g diced free range chicken breast
                    26, // 1 Tbsp honey
                }),
            // 12 matched ingredients
            ("Balanced Thai Pork Stir-Fry with Brown Rice",
                "1 1/2 cups brown rice\n5 carrot\n400g green beans\n2 can baby corn\n2 tbsp ginger\n3 spring onion\n1 drizzle of oil\n1400g chicken mince\n3 tbsp Thai yellow curry paste\n3 tbsp sweet chili sauce\n2 tbsp soy sauce\n1 pinch of chili flakes\n75g chopped cashew nuts",
                12,
                new[]
                {
                    48, // 1 1/2 cups brown rice
                    3, // 5 carrot
                    0, // 400g green beans
                    0, // 2 can baby corn
                    0, // 2 tbsp ginger
                    0, // 3 spring onion
                    0, // 1 drizzle of oil
                    0, // 1400g chicken mince
                    0, // 3 tbsp Thai yellow curry paste
                    0, // 3 tbsp sweet chili sauce
                    0, // 2 tbsp soy sauce
                    0, // 1 pinch of chili flakes
                    // 75g chopped cashew nuts — no match
                }),
            // 13 matched ingredients
            ("Satay Chicken & Veggie Curry with Rice & Broccoli",
                "1 drizzle of olive oil\n1 1/4 cups brown rice\n6 carrot\n2 head broccoli\n1300g diced chicken\n1 tbsp curry powder\n80g peanut butter\n500ml coconut milk\n1 tbsp chicken-style stock powder\n3 tbsp soy sauce\n2 tbsp brown sugar\n3/4 cup water\n3 tsp white wine vinegar",
                13,
                new[]
                {
                    0, // 1 drizzle of olive oil
                    0, // 1 1/4 cups brown rice
                    0, // 6 carrot
                    0, // 2 head broccoli
                    0, // 1300g diced chicken
                    0, // 1 tbsp curry powder
                    0, // 80g peanut butter
                    0, // 500ml coconut milk
                    0, // 1 tbsp chicken-style stock powder
                    0, // 3 tbsp soy sauce
                    0, // 2 tbsp brown sugar
                    0, // 3/4 cup water
                    0, // 3 tsp white wine vinegar
                })
        };
        
        // Act & Assert
        foreach ((string name, string ingredients, int expectedMatches, int[] expectedFoodIds) in testRecipes)
        {
            List<ParsedIngredientMatch> parsedIngredients = await _ingredientParserService.ParseAndMatchIngredientsAsync(ingredients);
            Assert.IsNotNull(parsedIngredients, $"Parsed ingredients should not be null for recipe: {name}");
            Assert.AreEqual(expectedMatches, parsedIngredients.Count, $"Expected {expectedMatches} matches for recipe: {name}, but got {parsedIngredients.Count}. Parsed ingredients: {string.Join(" - ", parsedIngredients.Select(parsedIngredientMatch => parsedIngredientMatch.MatchedFood.Name))}");
            foreach (var parsedIngredient in parsedIngredients)
            {
                Assert.IsTrue(parsedIngredient.IsMatched, $"Ingredient '{parsedIngredient.ParsedIngredient.RawLine}' in recipe '{name}' was not matched to any food item.");
            }
            
            int[] actualFoodIds = parsedIngredients.Select(p => p.MatchedFood!.Id).ToArray();
            CollectionAssert.AreEqual(
                expectedFoodIds,
                actualFoodIds,
                $"Matched food IDs did not match expected IDs for recipe: {name}. " +
                $"Actual matches: {string.Join(", ", parsedIngredients.Select(p => $"{p.ParsedIngredient.RawLine} ? [{p.MatchedFood!.Id}] {p.MatchedFood.Name}"))}");
        }
    }
}
