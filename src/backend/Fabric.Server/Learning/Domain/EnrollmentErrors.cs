namespace Fabric.Server.Learning.Domain;

public enum EnrollmentErrors
{
    EnrollmentNotFound,
    CourseNotFound,
    IdentityNotFound,
    ActiveEnrollmentAlreadyExists,
    EnrollmentAlreadyCompleted,
    EnrollmentAlreadyCancelled,
    EnrollmentNotActive,
    LaunchSessionNotFound,
    LaunchSessionExpired,
    LaunchSessionTokenInvalid,
    LaunchSessionAlreadyCompleted,
    CourseVersionNotFound,
}
