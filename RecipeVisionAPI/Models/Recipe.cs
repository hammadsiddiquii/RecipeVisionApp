namespace RecipeVisionAPI.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> RequiredIngredients { get; set; } = new List<string>();
        public string Instructions { get; set; }
        public string ImageUrl { get; set; }
    }
}
