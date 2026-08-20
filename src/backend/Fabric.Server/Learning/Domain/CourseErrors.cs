namespace Fabric.Server.Learning.Domain;

public enum CourseErrors
{
    CourseNotFound,
    CourseCodeRequired,
    CourseTitleRequired,
    CourseCodeAlreadyExists,
    CourseLanguageNotFound,
    CourseLanguageCodeRequired,
    CourseLanguageDisplayLabelRequired,
    CourseLanguageAlreadyExists,
    CourseAlreadyActive,
    CourseAlreadyInactive,
    InvalidPackage,
    ManifestNotFound,
    NoLaunchableScoFound,
    PackageStorageFailed,
}
