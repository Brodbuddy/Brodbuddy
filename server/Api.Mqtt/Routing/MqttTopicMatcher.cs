namespace Api.Mqtt.Routing;

public static class MqttTopicMatcher
{
    /// <summary>
    /// Tjekker om et published topic matcher et subscription topic filter
    /// </summary>
    /// <param name="topicFilter">Subscription topic filteret (kan indeholde wildcards)</param>
    /// <param name="actualTopic">Det faktiske published topic (ingen wildcards)</param>
    /// <returns>True hvis actualTopic mathcer topicFilter mønsteret</returns>
    public static bool Matches(string topicFilter, string actualTopic)
    {
        if (string.IsNullOrEmpty(topicFilter)) return false;

        // Gør at "#" matcher alt inkl. tomme topics.
        if (topicFilter == "#") return true;
        
        if (string.IsNullOrEmpty(actualTopic)) return false;
        
        if (string.Equals(topicFilter, actualTopic, StringComparison.Ordinal)) return true;
        if (!topicFilter.Contains('+') && !topicFilter.Contains('#')) return false;

        string[] filterSegments = topicFilter.Split('/');
        string[] topicSegments = actualTopic.Split('/');

        // Håndter # wildcard - if filteret slutter med #, matcher det hvilket som helst topic
        // med det samme præfiks (kan være tomt hvis filter bare er '#')
        if (filterSegments[^1] == "#")
        {
            if (filterSegments.Length == 1)
                return true;

            int segmentsToCheck = filterSegments.Length - 1;
            if (topicSegments.Length < segmentsToCheck)
                return false;

            for (int i = 0; i < segmentsToCheck; i++)
            {
                if (filterSegments[i] != "+" &&
                    !string.Equals(filterSegments[i], topicSegments[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        // Hvis vi er her, så er der ikke noget # wildcard i slutningen
        // Topic segments skal matche filter segments nøjagtigt 
        if (filterSegments.Length != topicSegments.Length) return false;

        for (int i = 0; i < filterSegments.Length; i++)
        {
            if (filterSegments[i] != "+" && !string.Equals(filterSegments[i], topicSegments[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}