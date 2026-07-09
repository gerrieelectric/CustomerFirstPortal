namespace Portal.Application.DTOs.Analytics;

public class RecognitionByNomineeDto
{
    public List<NomineeRecognitionItem> TopNominees { get; set; } = [];
    public DateTime GeneratedUtc { get; set; }

    public class NomineeRecognitionItem
    {
        public string NomineeName { get; set; } = string.Empty;
        public string? NomineeEmail { get; set; }
        public string? Department { get; set; }
        public int TotalRecognitions { get; set; }
        public int ApprovedRecognitions { get; set; }
        public double AverageServiceRating { get; set; }
        public DateTime MostRecentRecognition { get; set; }
    }
}
