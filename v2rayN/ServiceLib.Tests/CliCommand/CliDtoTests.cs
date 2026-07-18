using System.Text.Json;
using AwesomeAssertions;
using ServiceLib.Models.Dto.CliCommand;
using Xunit;

namespace ServiceLib.Tests.CliCommand;

public class CliDtoTests
{
    [Fact]
    public void CliRequest_RoundTrip_SerializesCorrectly()
    {
        var request = new CliRequest
        {
            Cmd = "node.switch",
            Args = new Dictionary<string, object?> { ["name"] = "HK-01" },
            Version = "1.0.0",
            Id = "test-id",
        };

        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<CliRequest>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Cmd.Should().Be("node.switch");
        deserialized.Args["name"]?.ToString().Should().Be("HK-01");
        deserialized.Id.Should().Be("test-id");
    }

    [Fact]
    public void CliResponse_Fail_ContainsError()
    {
        var response = CliResponse.Fail("id", "not found");
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<CliResponse>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Success.Should().BeFalse();
        deserialized.Error.Should().Be("not found");
    }

    [Fact]
    public void CliResponse_Ok_ContainsData()
    {
        var response = CliResponse.Ok("id", new { foo = "bar" });
        response.Success.Should().BeTrue();
        response.Id.Should().Be("id");
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public void NodeListItem_Record_ExposesAllFields()
    {
        var item = new NodeListItem("idx1", "HK-01", "1.1.1.1", 443, "VMess", "120ms", "sub1", "My Sub");
        item.Id.Should().Be("idx1");
        item.Name.Should().Be("HK-01");
        item.Address.Should().Be("1.1.1.1");
        item.Port.Should().Be(443);
        item.Type.Should().Be("VMess");
        item.Delay.Should().Be("120ms");
        item.GroupId.Should().Be("sub1");
        item.GroupName.Should().Be("My Sub");
    }
}
