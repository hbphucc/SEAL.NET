using System.ComponentModel.DataAnnotations;
using SEAL.NET.DTOs.Round;

namespace SEAL.NET.Tests.DTOs.Round;

public class RoundRequestValidationTests
{
    [Fact]
    public void CreateRoundRequest_MaxTeamsAdvancingZero_IsInvalid()
    {
        var request = new CreateRoundRequest
        {
            RoundName = "Round 1",
            SubmissionDeadline = DateTime.UtcNow,
            RoundOrder = 1,
            MaxTeamsAdvancing = 0
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateRoundRequest.MaxTeamsAdvancing)));
    }

    [Fact]
    public void UpdateRoundRequest_MaxTeamsAdvancingZero_IsInvalid()
    {
        var request = new UpdateRoundRequest
        {
            RoundName = "Round 1",
            SubmissionDeadline = DateTime.UtcNow,
            RoundOrder = 1,
            MaxTeamsAdvancing = 0
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UpdateRoundRequest.MaxTeamsAdvancing)));
    }

    private static List<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
        return results;
    }
}
