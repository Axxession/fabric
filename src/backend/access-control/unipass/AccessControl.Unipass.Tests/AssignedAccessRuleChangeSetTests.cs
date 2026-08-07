using System.Net;
using AccessControl.Unipass.ChangeSets;
using AccessControl.Unipass.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;

namespace AccessControl.Unipass.Tests;

public sealed class AssignedAccessRuleChangeSetTests : UnipassTestBase
{
    [Fact]
    public async Task ApplyChangeSet_WhenAssignmentIsPermanent_OmitsStartAndEndTimeFields()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get
                    && r.RequestUri!.ToString().Contains("/IDtech/IdtAPIService/api/PersonAccessRules")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post
                    && r.RequestUri!.ToString().Contains("/IDtech/IdtAPIService/api/PersonAccessRules")
                    && r.Content != null
                    && !r.Content.ReadAsStringAsync().Result.Contains("StartDate")
                    && !r.Content.ReadAsStringAsync().Result.Contains("StartTime")
                    && !r.Content.ReadAsStringAsync().Result.Contains("EndDate")
                    && !r.Content.ReadAsStringAsync().Result.Contains("EndTime")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"Id\":\"1\",\"Success\":true}]")
            })
            .Verifiable();

        var sp = CreateServiceProvider(handlerMock.Object);
        var api = sp.GetRequiredService<IUnipassApi>();

        UnipassOperationResponse response = await api.ApplyChangeSet(
            AssignedAccessRuleChangeSet.Assign(120, 1, 2));

        Assert.True(response.Success);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post
                && r.RequestUri!.ToString().Contains("/IDtech/IdtAPIService/api/PersonAccessRules")),
            ItExpr.IsAny<CancellationToken>());
    }
}
