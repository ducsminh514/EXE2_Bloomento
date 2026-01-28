namespace ADHDChecklist.API.Entities.Exceptions
{
    public class PremiumFeatureException : Exception
    {
        public string FeatureName { get; }

        public PremiumFeatureException(string message, string featureName) : base(message)
        {
            FeatureName = featureName;
        }
    }
}
