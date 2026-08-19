using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.Learning.Contracts;
using Fabric.Server.Learning.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Learning;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(CourseResponse))]
[JsonSerializable(typeof(CourseResponse[]))]
[JsonSerializable(typeof(CourseLanguageResponse))]
[JsonSerializable(typeof(CourseLanguageResponse[]))]
[JsonSerializable(typeof(CourseVersionResponse))]
[JsonSerializable(typeof(CourseVersionResponse[]))]
[JsonSerializable(typeof(CourseScoResponse[]))]
[JsonSerializable(typeof(EnrollmentResponse))]
[JsonSerializable(typeof(EnrollmentResponse[]))]
[JsonSerializable(typeof(AttemptResponse))]
[JsonSerializable(typeof(AttemptResponse[]))]
[JsonSerializable(typeof(CourseCompletionReportRowResponse[]))]
[JsonSerializable(typeof(LaunchSessionResponse))]
[JsonSerializable(typeof(ScormProgressResponse))]
[JsonSerializable(typeof(ListCoursesRequest))]
[JsonSerializable(typeof(CreateCourseRequest))]
[JsonSerializable(typeof(CreateCourseLanguageRequest))]
[JsonSerializable(typeof(UpdateCourseLanguageRequest))]
[JsonSerializable(typeof(CreateCourseUploadRequest))]
[JsonSerializable(typeof(CreateCourseVersionUploadRequest))]
[JsonSerializable(typeof(UpdateCourseRequest))]
[JsonSerializable(typeof(CreateEnrollmentRequest))]
[JsonSerializable(typeof(CancelEnrollmentRequest))]
[JsonSerializable(typeof(ListEnrollmentsRequest))]
[JsonSerializable(typeof(CreateLaunchSessionRequest))]
[JsonSerializable(typeof(RecordScormProgressRequest))]
[JsonSerializable(typeof(Page<CourseResponse>))]
[JsonSerializable(typeof(Page<EnrollmentResponse>))]
[JsonSerializable(typeof(Page<AttemptResponse>))]
[JsonSerializable(typeof(IFormFile))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(EnrollmentStatus?))]
[JsonSerializable(typeof(EnrollmentStatus[]))]
[JsonSerializable(typeof(AttemptStatus?))]
[JsonSerializable(typeof(AttemptStatus[]))]
[JsonSerializable(typeof(ScormVersion))]
[JsonSerializable(typeof(ScormVersion?))]
[JsonSerializable(typeof(ScormVersion[]))]
internal sealed partial class LearningJsonSerializerContext : JsonSerializerContext;
