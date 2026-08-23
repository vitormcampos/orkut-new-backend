using App.Application.DTOs;
using App.Domain.Entities;
using Bogus;

namespace App.Application.Test.Fakers;

public static class CommunityFaker
{
    private static readonly Faker<Community> CommunityEntityFaker = new Faker<Community>()
        .CustomInstantiator(f => new Community(
            Guid.NewGuid(),
            f.Company.CompanyName(),
            f.Lorem.Sentence()
        ));

    private static readonly Faker<CommunityMember> CommunityMemberEntityFaker = new Faker<CommunityMember>()
        .CustomInstantiator(f => new CommunityMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            f.PickRandom<MembershipRole>()
        ));

    private static readonly Faker<CreateCommunityRequest> CreateRequestFaker = new Faker<CreateCommunityRequest>()
        .CustomInstantiator(f => new CreateCommunityRequest(
            f.Company.CompanyName(),
            f.Lorem.Sentence()
        ));

    private static readonly Faker<UpdateCommunityRequest> UpdateRequestFaker = new Faker<UpdateCommunityRequest>()
        .CustomInstantiator(f => new UpdateCommunityRequest(
            f.Company.CompanyName(),
            f.Lorem.Sentence()
        ));

    public static Community GenerateCommunity() => CommunityEntityFaker.Generate();
    public static List<Community> GenerateCommunities(int count) => CommunityEntityFaker.Generate(count);
    public static CommunityMember GenerateCommunityMember() => CommunityMemberEntityFaker.Generate();
    public static CreateCommunityRequest GenerateCreateRequest() => CreateRequestFaker.Generate();
    public static UpdateCommunityRequest GenerateUpdateRequest() => UpdateRequestFaker.Generate();
}
