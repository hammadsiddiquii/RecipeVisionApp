using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using RecipeVisionAPI.Models;

namespace RecipeVisionAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageAnalysisController : ControllerBase
    {
        // Inside the ImageAnalysisController class
        private List<Recipe> GetSampleRecipes()
        {
            return new List<Recipe>
    {
        new Recipe
        {
            Id = 1,
            Name = "Apple Crumble",
            RequiredIngredients = { "apple", "flour", "sugar", "butter", "cinnamon" },
            Instructions = "Slice apples, mix with sugar and cinnamon. Top with flour, sugar, butter crumble. Bake at 375F for 30-40 mins.",
            ImageUrl = "https://example.com/apple_crumble.jpg" // Placeholder image URL
        },
        new Recipe
        {
            Id = 2,
            Name = "Tomato Pasta",
            RequiredIngredients = { "tomato", "pasta", "onion", "garlic", "olive oil" },
            Instructions = "Cook pasta. Sauté onion and garlic, add diced tomatoes. Mix with pasta.",
            ImageUrl = "https://example.com/tomato_pasta.jpg" // Placeholder image URL
        },
        new Recipe
        {
            Id = 3,
            Name = "Scrambled Eggs",
            RequiredIngredients = { "egg", "butter", "milk" },
            Instructions = "Whisk eggs with milk. Melt butter in pan, pour eggs, scramble until cooked.",
            ImageUrl = "https://example.com/scrambled_eggs.jpg" // Placeholder image URL
        },
        new Recipe
        {
            Id = 4,
            Name = "Chicken Stir-fry",
            RequiredIngredients = { "chicken", "broccoli", "carrot", "soy sauce", "ginger", "garlic" },
            Instructions = "Cut chicken and vegetables. Stir-fry chicken, then add veggies. Add sauce and serve.",
            ImageUrl = "https://example.com/chicken_stirfry.jpg" // Placeholder image URL
        }
    };
        }

        // You will modify the AnalyzeImage method next

        private readonly string _azureVisionEndpoint;
        private readonly string _azureVisionKey;

        public ImageAnalysisController(IConfiguration configuration)
        {
            _azureVisionEndpoint = configuration["AzureVision:Endpoint"] ?? throw new ArgumentNullException("AzureVision:Endpoint not found in configuration.");
            _azureVisionKey = configuration["AzureVision:Key"] ?? throw new ArgumentNullException("AzureVision:Key not found in configuration.");
        }

        [HttpPost("analyze")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AnalyzeImage([FromForm] IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("No image file uploaded.");
            }

            ImageAnalysisClient client = new ImageAnalysisClient(
                new Uri(_azureVisionEndpoint),
                new AzureKeyCredential(_azureVisionKey));

            using (var stream = imageFile.OpenReadStream())
            {
                BinaryData imageData = BinaryData.FromStream(stream);

                VisualFeatures features = VisualFeatures.Objects; 

                try
                {
                    ImageAnalysisResult result = await client.AnalyzeAsync(
                        imageData,
                        features,
                        new ImageAnalysisOptions() // Options object is still used for other parameters
                    );

                    List<string> detectedIngredientNames = new List<string>();

                    if (result.Objects != null && result.Objects.Values.Count > 0)
                    {
                        // Collect all detected object names
                        foreach (var obj in result.Objects.Values)
                        {
                            if (obj.Tags.Any())
                            {
                                // Take the first tag name as the ingredient name
                                detectedIngredientNames.Add(obj.Tags.First().Name.ToLower());
                            }
                        }
                    }

                    // Get sample recipes
                    List<Recipe> sampleRecipes = GetSampleRecipes();

                    // Filter recipes based on detected ingredients
                    var matchingRecipes = sampleRecipes.Where(recipe =>
                        recipe.RequiredIngredients.Any(ingredient =>
                            detectedIngredientNames.Contains(ingredient.ToLower()))).ToList();

                    if (matchingRecipes.Count == 0)
                    {
                        return NotFound(new { Message = "No matching recipes found for the detected ingredients." });
                    }

                    return Ok(new
                    {
                        DetectedIngredients = detectedIngredientNames,
                        MatchingRecipes = matchingRecipes,
                        Message = "Image analysis completed successfully."
                    });

                }
                catch (RequestFailedException ex)
                {
                    return StatusCode(ex.Status, new { Error = ex.Message, Code = ex.ErrorCode });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Error = "An unexpected error occurred during image analysis.", Details = ex.Message });
                }
            }
        }
    }
}