# Improving Ingredient Matching in Broccoli.App

The `IngredientParserService` currently relies on a combination of regex-based parsing and Levenshtein distance for fuzzy matching. While effective for simple cases, the "failing test" highlights limitations when dealing with real-world ingredient descriptions that include modifiers, different word orders, or variations in terminology.

To make the failing test pass and significantly improve the robustness and accuracy of ingredient matching, the following techniques can be implemented:

## 1. Pre-processing of Food Names

Before performing any fuzzy matching, both the input `FoodName` (extracted from the ingredient line) and the `Food.Name` from the database should undergo a series of normalization and cleaning steps.

### a. Stop Word Removal
Many words in ingredient descriptions do not contribute to identifying the core food item and can increase the Levenshtein distance unnecessarily.
*   **Technique**: Maintain a list of common "stop words" (e.g., "diced", "grated", "fresh", "raw", "skinless", "free range", "optional", "a", "of", "the", "drizzle", "pack", "twin pack", "sliced", "cm"). Remove these words from both the input `FoodName` and the database `Food.Name` before comparison.
*   **Example**:
    *   "diced free range chicken breast" -> "chicken breast"
    *   "Chicken Breast, Skinless, Raw" -> "chicken breast"
    *   "vermicelli noodles" -> "vermicelli"
    *   "carrot, grated" -> "carrot"

### b. Stemming/Lemmatization
Reduce words to their base or root form.
*   **Technique**: Use a stemming algorithm (e.g., Porter Stemmer) or a lemmatizer to convert different forms of a word to a common base.
*   **Example**: "carrots" -> "carrot", "noodles" -> "noodle".

### c. Normalization
Handle variations in spelling, punctuation, and abbreviations.
*   **Technique**:
    *   Convert all text to lowercase.
    *   Remove extra spaces and punctuation (e.g., commas, periods, hyphens) unless they are significant for distinguishing food items.
    *   Standardize abbreviations (e.g., "tbsp" to "tablespoon", "tsp" to "teaspoon").
*   **Example**: "Carrots, Raw" -> "carrots raw" -> "carrot raw" (after stemming).

## 2. Improved Fuzzy Matching Algorithm

The current Levenshtein distance, while a good start, has limitations, especially with longer strings or when word order changes.

### a. Token-based Matching
Instead of comparing entire strings, break them into meaningful tokens (words) and compare the sets of tokens.
*   **Technique**:
    *   **Jaccard Index**: Calculate the similarity between two sets of tokens (words). This is robust to word order changes.
    *   **TF-IDF (Term Frequency-Inverse Document Frequency)**: Assign weights to words based on their importance and frequency, then compare vectors of these weights.
    *   **N-gram Matching**: Compare overlapping sequences of N characters or words.
*   **Example**: "chicken breast" vs "breast chicken" would have a high token-based similarity.

### b. Weighted Levenshtein Distance
Modify the Levenshtein algorithm to give different costs to different types of edits or positions.
*   **Technique**: Prioritize matches at the beginning of the string, or give lower cost to common typos.

### c. Soundex/Metaphone
For phonetic matching, useful for misspellings that sound similar but are spelled differently.
*   **Technique**: Convert words to a phonetic code and compare the codes.

## 3. Configuration for Fuzzy Matching

Allow for more flexible control over the fuzzy matching parameters.

*   **Technique**:
    *   **Configurable `maxDistance`**: Instead of a hardcoded value (like 10), allow the `maxDistance` to be set dynamically or be part of the `Food` model itself (e.g., a `FuzzyMatchThreshold` property).
    *   **Multiple Matching Strategies**: Allow the service to try different matching strategies (e.g., exact match, then token-based, then phonetic) and return the best match.

## 4. Contextual Matching

Leverage additional information beyond just the food name.

*   **Technique**:
    *   **Unit Consideration**: When matching, consider the unit. If the input is "250g chicken breast", prioritize database entries that have "Gram" as their `Measure` or a `GramsPerMeasure` that makes sense for the quantity.
    *   **Category/Tags**: If food items have categories or tags, use these to narrow down search results before applying fuzzy matching.
    *   **User Preferences/History**: Learn from user's past choices to prioritize certain matches.

## Conclusion

Implementing a combination of these techniques would significantly enhance the `IngredientParserService`'s ability to accurately match real-world ingredient descriptions to the food database, making the failing test pass and improving the overall user experience. This would likely involve a more complex matching pipeline than the current single-pass Levenshtein approach.
