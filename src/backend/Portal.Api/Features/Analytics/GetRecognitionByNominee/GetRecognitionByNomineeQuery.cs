using MediatR;
using Portal.Application.DTOs.Analytics;

namespace Portal.Application.Features.Analytics.GetRecognitionByNominee;

public class GetRecognitionByNomineeQuery : IRequest<RecognitionByNomineeDto>
{
    public int TopCount { get; set; } = 10; // Top 10 nominees by default
}
