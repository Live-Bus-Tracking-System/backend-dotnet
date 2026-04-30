namespace BusTracker.Application.Common.Helpers
{
    public static class StringMetrics
    {
        /// <summary>
        /// Calculates the Levenshtein distance between two strings.
        /// </summary>
        public static int CalculateLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(target) ? 0 : target.Length;

            if (string.IsNullOrEmpty(target))
                return source.Length;

            source = source.ToLowerInvariant();
            target = target.ToLowerInvariant();

            int sourceLength = source.Length;
            int targetLength = target.Length;

            var distance = new int[sourceLength + 1, targetLength + 1];

            for (int i = 0; i <= sourceLength; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetLength; distance[0, j] = j++) ;

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceLength, targetLength];
        }

        /// <summary>
        /// Calculates a fuzzy match score (0-100) based on Levenshtein distance and length.
        /// </summary>
        public static int CalculateFuzzyScore(string source, string target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                return 0;

            if (source.Equals(target, StringComparison.OrdinalIgnoreCase))
                return 100;

            int distance = CalculateLevenshteinDistance(source, target);
            int maxLength = Math.Max(source.Length, target.Length);

            if (maxLength == 0) return 100;

            // Score is percentage of characters that match
            double score = (1.0 - ((double)distance / maxLength)) * 100;
            return (int)Math.Max(0, score);
        }
    }
}
