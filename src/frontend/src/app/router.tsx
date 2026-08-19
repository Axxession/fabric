import { useTranslation } from 'react-i18next';
import { Navigate, Outlet, createRootRoute, createRoute, createRouter } from '@tanstack/react-router';
import { lazy, Suspense } from 'react';

import { AppLayout } from '@/shared/layout/app-layout';
import { ProtectedRoute } from '@/shared/auth/protected-route';
import { IntegratorRoute } from '@/features/integrations/integrator-route';
import { EmployeeContractorPlannerRoute } from '@/features/perspectives/employee-contractor-planner-route';
import { EmployeeHostRoute } from '@/features/perspectives/employee-host-route';
import { PerspectiveHomePage } from '@/features/perspectives/perspective-home-page';
import { DesfireStudioLayout } from '@/features/desfire-studio/desfire-studio-layout';
import { ReceptionKioskLayout } from '@/features/reception-kiosk/layout/reception-kiosk-layout';
import { ReceptionDeskWorkstationLayout } from '@/features/reception-desk/layout/reception-desk-workstation-layout';

const AccessControlPage = lazy(() => import('@/features/administration/access-control-page'));
const AccessControlSystemCreatePage = lazy(() => import('@/features/administration/access-control-system-create-page'));
const AccessControlSystemEditPage = lazy(() => import('@/features/administration/access-control-system-edit-page'));
const AccessControlTargetEditPage = lazy(() => import('@/features/administration/access-control-target-edit-page'));
const AccessItemCreatePage = lazy(() => import('@/features/administration/access-item-create-page'));
const AccessItemEditPage = lazy(() => import('@/features/administration/access-item-edit-page'));
const AccessModelPage = lazy(() => import('@/features/administration/access-model-page'));
const ApprovalGroupCreatePage = lazy(() => import('@/features/administration/approval-group-create-page'));
const ApprovalGroupEditPage = lazy(() => import('@/features/administration/approval-group-edit-page'));
const AuthCallbackPage = lazy(() => import('@/features/auth/auth-callback-page'));
const AutomationWorkflowDefinitionEditorPage = lazy(() => import('@/features/automation/workflow-definition-editor-page'));
const AutomationWorkflowInstanceViewerPage = lazy(() => import('@/features/automation/workflow-instance-viewer-page'));
const AutomationWorkflowPage = lazy(() => import('@/features/automation/workflow-page'));
const AutomationKioskPage = lazy(() => import('@/features/automation/kiosk-admin-page'));
const AutomationKioskEditPage = lazy(() => import('@/features/automation/kiosk-edit-page'));
const AutomationKioskProfileEditPage = lazy(() => import('@/features/automation/kiosk-profile-edit-page'));
const CatalogueCreatePage = lazy(() => import('@/features/administration/catalogue-create-page'));
const CatalogueEditPage = lazy(() => import('@/features/administration/catalogue-edit-page'));
const CardManagementChipDesignCreatePage = lazy(() => import('@/features/card-management/chip-design-create-page'));
const CardManagementChipDesignEditPageDesfireStudio = lazy(() => import('@/features/card-management/chip-design-edit-page-desfire-studio'));
const CardManagementChipDesignerPage = lazy(() => import('@/features/card-management/chip-designer-page'));
const CardManagementCardEditorPage = lazy(() => import('@/features/card-management/card-editor-page'));
const CardManagementPrintDesignCreatePage = lazy(() => import('@/features/card-management/print-design-create-page'));
const CardManagementPrintDesignEditPage = lazy(() => import('@/features/card-management/print-design-form-page'));
const CardManagementKeyGroupCreatePage = lazy(() => import('@/features/card-management/key-group-create-page'));
const CardManagementKeyGroupEditPageDesfireStudio = lazy(() => import('@/features/card-management/key-group-edit-page-desfire-studio'));
const CardManagementKeyManagementPage = lazy(() => import('@/features/card-management/key-management-page'));
const CardManagementPrintBatchCreatePage = lazy(() => import('@/features/card-management/print-batch-create-page'));
const CardManagementPrintBatchDetailPageDesfireStudio = lazy(() => import('@/features/card-management/print-batch-detail-page-desfire-studio'));
const CardManagementEncoderFormPage = lazy(() => import('@/features/card-management/encoder-form-page'));
const CardManagementEncoderFormPageDesfireStudio = lazy(() => import('@/features/card-management/encoder-form-page-desfire-studio'));
const CardManagementPrintRunDetailPageDesfireStudio = lazy(() => import('@/features/card-management/print-run-detail-page-desfire-studio'));
const CardManagementPrintingPage = lazy(() => import('@/features/card-management/printing-page'));
const CardManagementStrategyCreatePage = lazy(() => import('@/features/card-management/diversification-strategy-create-page'));
const CardManagementStrategyEditPageDesfireStudio = lazy(() => import('@/features/card-management/diversification-strategy-edit-page-desfire-studio'));
const CardManagementSystemProviderCreatePage = lazy(() => import('@/features/card-management/system-provider-create-page'));
const CardManagementTransformationCreatePage = lazy(() => import('@/features/card-management/transformation-create-page'));
const CardManagementTransformationEditPageDesfireStudio = lazy(() => import('@/features/card-management/transformation-edit-page-desfire-studio'));
const ClientsPage = lazy(() => import('@/features/administration/clients-page'));
const ContractorJobTypeCreatePage = lazy(() => import('@/features/administration/contractor-job-type-create-page'));
const ContractorJobTypeEditPage = lazy(() => import('@/features/administration/contractor-job-type-edit-page'));
const CredentialTypesPage = lazy(() => import('@/features/administration/credential-types-page'));
const CredentialTypeCreatePage = lazy(() => import('@/features/administration/credential-type-create-page'));
const CredentialTypeEditPage = lazy(() => import('@/features/administration/credential-type-edit-page'));
const DesfireStudioHardwareAgentDetailPage = lazy(() => import('@/features/desfire-studio/desfire-studio-hardware-agent-detail-page'));
const DesfireStudioHardwareAgentsPage = lazy(() => import('@/features/desfire-studio/desfire-studio-hardware-agents-page'));
const DesfireStudioPage = lazy(() => import('@/features/desfire-studio/desfire-studio-page'));
const EmployeeCreatePage = lazy(() => import('@/features/administration/employee-create-page'));
const EmployeeEditPage = lazy(() => import('@/features/administration/employee-edit-page'));
const IdentitiesPage = lazy(() => import('@/features/identities/identities-page'));
const IntegrationsKeycloakPage = lazy(() => import('@/features/integrations/keycloak-page'));
const IntegrationsMicrosoftGraphPage = lazy(() => import('@/features/integrations/microsoft-graph-page'));
const UserManagementPage = lazy(() => import('@/features/administration/user-management-page'));
const KeycloakUserCreatePage = lazy(() => import('@/features/administration/keycloak-user-create-page'));
const KeycloakUserEditPage = lazy(() => import('@/features/administration/keycloak-user-edit-page'));
const KeycloakRoleCreatePage = lazy(() => import('@/features/administration/keycloak-role-create-page'));
const KeycloakRoleEditPage = lazy(() => import('@/features/administration/keycloak-role-edit-page'));
const KeycloakGroupCreatePage = lazy(() => import('@/features/administration/keycloak-group-create-page'));
const KeycloakGroupEditPage = lazy(() => import('@/features/administration/keycloak-group-edit-page'));
const FacilityHardwareAgentDetailPage = lazy(() => import('@/features/facility/hardware-agent-detail-page'));
const FacilityBuildingEditPage = lazy(() => import('@/features/facility/building-edit-page'));
const FacilityRoomEditPage = lazy(() => import('@/features/facility/room-edit-page'));
const FacilitySiteCreatePage = lazy(() => import('@/features/facility/site-create-page'));
const FacilitySiteEditPage = lazy(() => import('@/features/facility/site-edit-page'));
const FacilityLocationsPage = lazy(() => import('@/features/facility/locations-page'));
const HomePage = lazy(() => import('@/features/home/home-page'));
const MyOrganizationPage = lazy(() => import('@/features/administration/my-organization-page'));
const NotificationsPage = lazy(() => import('@/features/administration/notifications-page'));
const OrganizationUnitCreatePage = lazy(() => import('@/features/administration/organization-unit-create-page'));
const OrganizationUnitEditPage = lazy(() => import('@/features/administration/organization-unit-edit-page'));
const EmployeeRequestAccessPage = lazy(() => import('@/features/perspectives/employee-request-access-page'));
const EmployeeContractorAssignmentCreatePage = lazy(() => import('@/features/perspectives/employee-contractor-assignment-create-page'));
const EmployeeContractorAssignmentDetailPage = lazy(() => import('@/features/perspectives/employee-contractor-assignment-detail-page'));
const EmployeeContractorJobCreatePage = lazy(() => import('@/features/perspectives/employee-contractor-job-create-page'));
const EmployeeContractorJobDetailPage = lazy(() => import('@/features/perspectives/employee-contractor-job-detail-page'));
const EmployeeContractorCompanyCreatePage = lazy(() => import('@/features/perspectives/employee-contractor-company-create-page'));
const EmployeeContractorCompanyDetailPage = lazy(() => import('@/features/perspectives/employee-contractor-company-detail-page'));
const EmployeeContractorCreatePage = lazy(() => import('@/features/perspectives/employee-contractor-create-page'));
const EmployeeContractorDetailPage = lazy(() => import('@/features/perspectives/employee-contractor-detail-page'));
const EmployeeContractorsPage = lazy(() => import('@/features/perspectives/employee-contractors-page'));
const EmployeeRequestDetailPage = lazy(() => import('@/features/perspectives/employee-request-detail-page'));
const EmployeeVisitorsPage = lazy(() => import('@/features/perspectives/employee-visitors-page'));
const EmployeeVisitEditPage = lazy(() => import('@/features/perspectives/employee-visit-edit-page'));
const ManagerApprovalInboxPage = lazy(() => import('@/features/perspectives/manager-approval-inbox-page'));
const ManagerMyTeamPage = lazy(() => import('@/features/perspectives/manager-my-team-page'));
const ManagerTeamMemberDetailPage = lazy(() => import('@/features/perspectives/manager-team-member-detail-page'));
const IdentityDetailPage = lazy(() => import('@/features/identities/identity-detail-page'));
const KioskPage = lazy(() => import('@/features/kiosk/kiosk-page'));
const KioskSetupPage = lazy(() => import('@/features/kiosk/kiosk-setup-page'));
const LmsPage = lazy(() => import('@/features/administration/lms-page'));
const LmsCourseCreatePage = lazy(() => import('@/features/administration/lms-course-create-page'));
const LmsCourseEditPage = lazy(() => import('@/features/administration/lms-course-edit-page'));
const LmsCourseLanguageCreatePage = lazy(() => import('@/features/administration/lms-course-language-create-page'));
const LmsCourseLanguageEditPage = lazy(() => import('@/features/administration/lms-course-language-edit-page'));
const LmsEnrollmentCreatePage = lazy(() => import('@/features/administration/lms-enrollment-create-page'));
const LmsCourseRequirementCreatePage = lazy(() => import('@/features/administration/lms-course-requirement-create-page'));
const LmsCourseRequirementEditPage = lazy(() => import('@/features/administration/lms-course-requirement-edit-page'));
const PackageCreatePage = lazy(() => import('@/features/administration/package-create-page'));
const PackageEditPage = lazy(() => import('@/features/administration/package-edit-page'));
const RequirementCreatePage = lazy(() => import('@/features/administration/requirement-create-page'));
const RequirementEditPage = lazy(() => import('@/features/administration/requirement-edit-page'));
const PersonaCreatePage = lazy(() => import('@/features/administration/persona-create-page'));
const PersonaEditPage = lazy(() => import('@/features/administration/persona-edit-page'));
const ReceptionDeskArrivalsPage = lazy(() => import('@/features/reception-desk/reception-desk-arrivals-page'));
const ReceptionDeskExpectedArrivalsPage = lazy(() => import('@/features/reception-desk/reception-desk-expected-arrivals-page'));
const ReceptionDeskHistoryPage = lazy(() => import('@/features/reception-desk/reception-desk-history-page'));
const ReceptionDeskWorkstationPage = lazy(() => import('@/features/reception-desk/reception-desk-workstation-page'));
const ReceptionDeskWorkstationSetupPage = lazy(() => import('@/features/reception-desk/reception-desk-workstation-setup-page'));
const ReceptionKioskArrivalPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-arrival-page'));
const ReceptionKioskDocumentScanPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-document-scan-page'));
const ReceptionKioskFaceScanPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-face-scan-page'));
const ReceptionKioskFailedPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-failed-page'));
const ReceptionKioskNoRegistrationPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-no-registration-page'));
const ReceptionKioskPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-page'));
const ReceptionKioskScanQrPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-scan-qr-page'));
const ReceptionKioskSetupPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-setup-page'));
const ReceptionKioskSuccessPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-success-page'));
const ReceptionKioskWrongLocationPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-wrong-location-page'));
const VisitorConfirmationPage = lazy(() => import('@/features/visitor-confirmation/visitor-confirmation-page'));
const VisitCreatePage = lazy(() => import('@/features/visitors-management/visit-create-page'));
const VisitInvitationDetailPage = lazy(() => import('@/features/visitors-management/visit-invitation-detail-page'));

const rootRoute = createRootRoute({
  component: () => (
    <Outlet />
  ),
});

const mainLayoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: 'main',
  component: () => (
    <AppLayout>
      <Outlet />
    </AppLayout>
  ),
});

const receptionKioskLayoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/reception-kiosk',
  component: () => (
    <ReceptionKioskLayout>
      <Outlet />
    </ReceptionKioskLayout>
  ),
});

const receptionDeskWorkstationLayoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/reception-desk-workstation',
  component: () => (
    <ReceptionDeskWorkstationLayout>
      <Outlet />
    </ReceptionDeskWorkstationLayout>
  ),
});

const desfireStudioLayoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/desfire-studio',
  component: () => (
    <DesfireStudioLayout>
      <Outlet />
    </DesfireStudioLayout>
  ),
});

const kioskLayoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/kiosk',
  component: () => <Outlet />,
});

const indexRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/',
  component: () => <LazyRoute component={<HomePage />} />,
});

const authCallbackRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/auth/callback',
  component: () => <LazyRoute component={<AuthCallbackPage />} />,
});

const employeeRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee',
  component: () => <ProtectedRoute><PerspectiveHomePage perspectiveId="employee" /></ProtectedRoute>,
});

const employeeRequestAccessRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/request-access',
  component: () => <ProtectedLazyRoute component={<EmployeeRequestAccessPage />} />,
});

const employeeRequestDetailRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/request-access/$requestId',
  component: () => <ProtectedLazyRoute component={<EmployeeRequestDetailPage />} />,
});

const employeeContractorsRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/contractors',
  component: () => <ProtectedRoute><EmployeeContractorPlannerRoute><LazyRoute component={<EmployeeContractorsPage />} /></EmployeeContractorPlannerRoute></ProtectedRoute>,
});

const employeeContractorCompanyCreateRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/contractors/companies/new',
  component: () => <ProtectedRoute><EmployeeContractorPlannerRoute><LazyRoute component={<EmployeeContractorCompanyCreatePage />} /></EmployeeContractorPlannerRoute></ProtectedRoute>,
});

const employeeContractorCompanyDetailRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/contractors/companies/$companyId',
  component: () => <ProtectedRoute><EmployeeContractorPlannerRoute><LazyRoute component={<EmployeeContractorCompanyDetailPage />} /></EmployeeContractorPlannerRoute></ProtectedRoute>,
});

const employeeContractorCreateRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/contractors/companies/$companyId/contractors/new',
  component: () => <ProtectedRoute><EmployeeContractorPlannerRoute><LazyRoute component={<EmployeeContractorCreatePage />} /></EmployeeContractorPlannerRoute></ProtectedRoute>,
});

const employeeContractorDetailRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/contractors/companies/$companyId/contractors/$contractorId',
  component: () => <ProtectedRoute><EmployeeContractorPlannerRoute><LazyRoute component={<EmployeeContractorDetailPage />} /></EmployeeContractorPlannerRoute></ProtectedRoute>,
});

const employeeContractorJobCreateRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/contractors/jobs/new',
  component: () => <ProtectedRoute><EmployeeContractorPlannerRoute><LazyRoute component={<EmployeeContractorJobCreatePage />} /></EmployeeContractorPlannerRoute></ProtectedRoute>,
});

const employeeContractorJobDetailRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/contractors/jobs/$jobId',
  component: () => <ProtectedRoute><EmployeeContractorPlannerRoute><LazyRoute component={<EmployeeContractorJobDetailPage />} /></EmployeeContractorPlannerRoute></ProtectedRoute>,
});

const employeeContractorAssignmentCreateRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/contractors/jobs/$jobId/assignments/new',
  component: () => <ProtectedRoute><EmployeeContractorPlannerRoute><LazyRoute component={<EmployeeContractorAssignmentCreatePage />} /></EmployeeContractorPlannerRoute></ProtectedRoute>,
});

const employeeContractorAssignmentDetailRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/contractors/jobs/$jobId/assignments/$assignmentId',
  component: () => <ProtectedRoute><EmployeeContractorPlannerRoute><LazyRoute component={<EmployeeContractorAssignmentDetailPage />} /></EmployeeContractorPlannerRoute></ProtectedRoute>,
});

const employeeVisitorsRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/visitors',
  component: () => <ProtectedRoute><EmployeeHostRoute><LazyRoute component={<EmployeeVisitorsPage />} /></EmployeeHostRoute></ProtectedRoute>,
});

const employeeVisitCreateRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/visitors/new',
  component: () => <ProtectedRoute><EmployeeHostRoute><LazyRoute component={<VisitCreatePage />} /></EmployeeHostRoute></ProtectedRoute>,
});

const employeeVisitEditRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/visitors/$visitId/edit',
  component: () => <ProtectedRoute><EmployeeHostRoute><LazyRoute component={<EmployeeVisitEditPage />} /></EmployeeHostRoute></ProtectedRoute>,
});

const employeeVisitInvitationDetailRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/employee/visitors/$visitId/invitations/$invitationId',
  component: () => <ProtectedRoute><EmployeeHostRoute><LazyRoute component={<VisitInvitationDetailPage />} /></EmployeeHostRoute></ProtectedRoute>,
});

const managerApprovalInboxRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/manager/approval-inbox',
  component: () => <ProtectedLazyRoute component={<ManagerApprovalInboxPage />} />,
});

const managerApprovalInboxDetailRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/manager/approval-inbox/$requestId',
  component: () => <ProtectedLazyRoute component={<EmployeeRequestDetailPage />} />,
});

const managerMyTeamRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/manager/my-team',
  component: () => <ProtectedLazyRoute component={<ManagerMyTeamPage />} />,
});

const managerTeamMemberDetailRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/manager/my-team/$employeeId',
  component: () => <ProtectedLazyRoute component={<ManagerTeamMemberDetailPage />} />,
});

const managerRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/manager',
  component: () => <ProtectedRoute><PerspectiveHomePage perspectiveId="manager" /></ProtectedRoute>,
});

const securityOfficerRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/security-officer',
  component: () => <ProtectedRoute><PerspectiveHomePage perspectiveId="security-officer" /></ProtectedRoute>,
});

const securityOfficerIdentitiesRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/security-officer/identities',
  component: () => <ProtectedLazyRoute component={<IdentitiesPage />} />,
});

const securityOfficerIdentityDetailRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/security-officer/identities/$identityId',
  component: () => <ProtectedLazyRoute component={<IdentityDetailPage />} />,
});

const securityOfficerIdentityRequestDetailRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/security-officer/identities/$identityId/requests/$requestId',
  component: () => <ProtectedLazyRoute component={<EmployeeRequestDetailPage />} />,
});

const integrationsRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/integrations',
  component: () => (
    <ProtectedRoute>
      <IntegratorRoute>
        <Outlet />
      </IntegratorRoute>
    </ProtectedRoute>
  ),
});

const integrationsIndexRoute = createRoute({
  getParentRoute: () => integrationsRoute,
  path: '/',
  component: () => <Navigate to="/integrations/microsoft-graph" replace />,
});

const integrationsMicrosoftGraphRoute = createRoute({
  getParentRoute: () => integrationsRoute,
  path: '/microsoft-graph',
  component: () => <LazyRoute component={<IntegrationsMicrosoftGraphPage />} />,
});

const integrationsKeycloakRoute = createRoute({
  getParentRoute: () => integrationsRoute,
  path: '/keycloak',
  component: () => <LazyRoute component={<IntegrationsKeycloakPage />} />,
});

const administrationRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/administration',
  component: () => (
    <ProtectedRoute>
      <Outlet />
    </ProtectedRoute>
  ),
});

const administrationIndexRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/',
  component: () => <Navigate to="/administration/sites" replace />,
});

const administrationSitesRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/sites',
  component: () => <LazyRoute component={<FacilityLocationsPage />} />,
});

const administrationClientsRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/clients',
  component: () => <LazyRoute component={<ClientsPage />} />,
});

const administrationAutomationRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/automation',
  component: () => (
    <ProtectedRoute>
      <Outlet />
    </ProtectedRoute>
  ),
});

const administrationAutomationIndexRoute = createRoute({
  getParentRoute: () => administrationAutomationRoute,
  path: '/',
  component: () => <Navigate to="/administration/automation/workflow" search={{ tab: 'definitions' } as never} replace />,
});

const administrationAutomationWorkflowRoute = createRoute({
  getParentRoute: () => administrationAutomationRoute,
  path: '/workflow',
  component: () => <LazyRoute component={<AutomationWorkflowPage />} />,
});

const administrationAutomationWorkflowDefinitionsRoute = createRoute({
  getParentRoute: () => administrationAutomationRoute,
  path: '/workflow-definitions',
  component: () => <Navigate to="/administration/automation/workflow" search={{ tab: 'definitions' } as never} replace />,
});

const administrationAutomationWorkflowDefinitionEditorRoute = createRoute({
  getParentRoute: () => administrationAutomationRoute,
  path: '/workflow-definitions/$definitionId/edit',
  component: () => <LazyRoute component={<AutomationWorkflowDefinitionEditorPage />} />,
});

const administrationAutomationWorkflowInstancesRoute = createRoute({
  getParentRoute: () => administrationAutomationRoute,
  path: '/workflow-instances',
  component: () => <Navigate to="/administration/automation/workflow" search={{ tab: 'history' } as never} replace />,
});

const administrationAutomationWorkflowInstanceViewerRoute = createRoute({
  getParentRoute: () => administrationAutomationRoute,
  path: '/workflow-instances/$instanceId',
  component: () => <LazyRoute component={<AutomationWorkflowInstanceViewerPage />} />,
});

const administrationAutomationKioskRoute = createRoute({
  getParentRoute: () => administrationAutomationRoute,
  path: '/kiosk',
  component: () => <LazyRoute component={<AutomationKioskPage />} />,
});

const administrationAutomationKioskEditRoute = createRoute({
  getParentRoute: () => administrationAutomationRoute,
  path: '/kiosk/$kioskId/edit',
  component: () => <LazyRoute component={<AutomationKioskEditPage />} />,
});

const administrationAutomationKioskProfileEditRoute = createRoute({
  getParentRoute: () => administrationAutomationRoute,
  path: '/kiosk/profiles/$profileId/edit',
  component: () => <LazyRoute component={<AutomationKioskProfileEditPage />} />,
});

const administrationAccessModelRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model',
  component: () => <LazyRoute component={<AccessModelPage />} />,
});

const administrationLmsRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/lms',
  component: () => <LazyRoute component={<LmsPage />} />,
});

const administrationLmsCourseCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/lms/courses/new',
  component: () => <LazyRoute component={<LmsCourseCreatePage />} />,
});

const administrationLmsCourseEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/lms/courses/$courseId',
  component: () => <LazyRoute component={<LmsCourseEditPage />} />,
});

const administrationLmsCourseLanguageCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/lms/courses/$courseId/languages/new',
  component: () => <LazyRoute component={<LmsCourseLanguageCreatePage />} />,
});

const administrationLmsCourseLanguageEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/lms/courses/$courseId/languages/$languageId',
  component: () => <LazyRoute component={<LmsCourseLanguageEditPage />} />,
});

const administrationLmsEnrollmentCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/lms/courses/$courseId/enrollments/new',
  component: () => <LazyRoute component={<LmsEnrollmentCreatePage />} />,
});

const administrationLmsCourseRequirementCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/lms/course-requirements/new',
  component: () => <LazyRoute component={<LmsCourseRequirementCreatePage />} />,
});

const administrationLmsCourseRequirementEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/lms/course-requirements/$ruleId',
  component: () => <LazyRoute component={<LmsCourseRequirementEditPage />} />,
});

const administrationAccessControlRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-control',
  component: () => <LazyRoute component={<AccessControlPage />} />,
});

const administrationUserManagementRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/user-management',
  component: () => <LazyRoute component={<UserManagementPage />} />,
});

const administrationUserManagementUserCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/user-management/users/new',
  component: () => <LazyRoute component={<KeycloakUserCreatePage />} />,
});

const administrationUserManagementUserEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/user-management/users/$userId/edit',
  component: () => <LazyRoute component={<KeycloakUserEditPage />} />,
});

const administrationUserManagementRoleCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/user-management/roles/new',
  component: () => <LazyRoute component={<KeycloakRoleCreatePage />} />,
});

const administrationUserManagementRoleEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/user-management/roles/$roleId/edit',
  component: () => <LazyRoute component={<KeycloakRoleEditPage />} />,
});

const administrationUserManagementGroupCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/user-management/groups/new',
  component: () => <LazyRoute component={<KeycloakGroupCreatePage />} />,
});

const administrationUserManagementGroupEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/user-management/groups/$groupId/edit',
  component: () => <LazyRoute component={<KeycloakGroupEditPage />} />,
});

const administrationCredentialTypesRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/credential-types',
  component: () => <LazyRoute component={<CredentialTypesPage />} />,
});

const administrationNotificationsRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/notifications',
  component: () => <LazyRoute component={<NotificationsPage />} />,
});

const administrationAccessItemCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-control/items/new',
  component: () => <LazyRoute component={<AccessItemCreatePage />} />,
});

const administrationAccessItemEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-control/items/$itemId/edit',
  component: () => <LazyRoute component={<AccessItemEditPage />} />,
});

const administrationAccessControlTargetEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-control/items/$itemId/targets/$targetId/edit',
  component: () => <LazyRoute component={<AccessControlTargetEditPage />} />,
});

const administrationAccessControlSystemCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-control/systems/new',
  component: () => <LazyRoute component={<AccessControlSystemCreatePage />} />,
});

const administrationAccessControlSystemEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-control/systems/$systemId/edit',
  component: () => <LazyRoute component={<AccessControlSystemEditPage />} />,
});

const administrationPackageCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model/packages/new',
  component: () => <LazyRoute component={<PackageCreatePage />} />,
});

const administrationCredentialTypeCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/credential-types/new',
  component: () => <LazyRoute component={<CredentialTypeCreatePage />} />,
});

const administrationCredentialTypeEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/credential-types/$credentialTypeId/edit',
  component: () => <LazyRoute component={<CredentialTypeEditPage />} />,
});

const administrationPackageEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model/packages/$packageId/edit',
  component: () => <LazyRoute component={<PackageEditPage />} />,
});

const administrationRequirementCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model/compliancy/new',
  component: () => <LazyRoute component={<RequirementCreatePage />} />,
});

const administrationRequirementEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model/compliancy/$requirementId/edit',
  component: () => <LazyRoute component={<RequirementEditPage />} />,
});

const administrationCatalogueCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model/catalogues/new',
  component: () => <LazyRoute component={<CatalogueCreatePage />} />,
});

const administrationCatalogueEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model/catalogues/$catalogueId/edit',
  component: () => <LazyRoute component={<CatalogueEditPage />} />,
});

const administrationApprovalGroupCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model/approval-groups/new',
  component: () => <LazyRoute component={<ApprovalGroupCreatePage />} />,
});

const administrationApprovalGroupEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model/approval-groups/$approvalGroupId/edit',
  component: () => <LazyRoute component={<ApprovalGroupEditPage />} />,
});

const administrationMyOrganizationRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/my-organization',
  component: () => <LazyRoute component={<MyOrganizationPage />} />,
});

const administrationContractorJobTypeCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/my-organization/contractor-job-types/new',
  component: () => <LazyRoute component={<ContractorJobTypeCreatePage />} />,
});

const administrationContractorJobTypeEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/my-organization/contractor-job-types/$jobTypeId/edit',
  component: () => <LazyRoute component={<ContractorJobTypeEditPage />} />,
});

const administrationEmployeeCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/my-organization/employees/new',
  component: () => <LazyRoute component={<EmployeeCreatePage />} />,
});

const administrationEmployeeEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/my-organization/employees/$employeeId/edit',
  component: () => <LazyRoute component={<EmployeeEditPage />} />,
});

const administrationOrganizationUnitCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/my-organization/organizational-units/new',
  component: () => <LazyRoute component={<OrganizationUnitCreatePage />} />,
});

const administrationOrganizationUnitEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/my-organization/organizational-units/$organizationUnitId/edit',
  component: () => <LazyRoute component={<OrganizationUnitEditPage />} />,
});

const administrationPersonaCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/my-organization/personas/new',
  component: () => <LazyRoute component={<PersonaCreatePage />} />,
});

const administrationPersonaEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/my-organization/personas/$personaId/edit',
  component: () => <LazyRoute component={<PersonaEditPage />} />,
});

const administrationSiteCreateRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/sites/new',
  component: () => <LazyRoute component={<FacilitySiteCreatePage />} />,
});

const administrationSiteEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/sites/$siteId/edit',
  component: () => <LazyRoute component={<FacilitySiteEditPage />} />,
});

const administrationBuildingEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/sites/$siteId/buildings/$buildingId/edit',
  component: () => <LazyRoute component={<FacilityBuildingEditPage />} />,
});

const administrationRoomEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/sites/$siteId/buildings/$buildingId/rooms/$roomId/edit',
  component: () => <LazyRoute component={<FacilityRoomEditPage />} />,
});

const administrationHardwareAgentDetailRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/clients/hardware-agents/$agentId',
  component: () => <LazyRoute component={<FacilityHardwareAgentDetailPage />} />,
});

const desfireStudioIndexRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/',
  component: () => <LazyRoute component={<DesfireStudioPage />} />,
});

const desfireStudioHardwareAgentsRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/hardware-agents',
  component: () => <ProtectedLazyRoute component={<DesfireStudioHardwareAgentsPage />} />,
});

const desfireStudioHardwareAgentDetailRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/hardware-agents/$agentId',
  component: () => <ProtectedLazyRoute component={<DesfireStudioHardwareAgentDetailPage />} />,
});

const desfireStudioKeyManagementRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/key-management',
  component: () => <ProtectedLazyRoute component={<CardManagementKeyManagementPage />} />,
});

const desfireStudioKeyGroupCreateRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/key-groups/new',
  component: () => <ProtectedLazyRoute component={<CardManagementKeyGroupCreatePage />} />,
});

const desfireStudioKeyGroupEditRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/key-groups/$keyGroupId/edit',
  component: () => <ProtectedLazyRoute component={<CardManagementKeyGroupEditPageDesfireStudio />} />,
});

const desfireStudioStrategyCreateRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/diversification-strategies/new',
  component: () => <ProtectedLazyRoute component={<CardManagementStrategyCreatePage />} />,
});

const desfireStudioStrategyEditRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/diversification-strategies/$strategyId/edit',
  component: () => <ProtectedLazyRoute component={<CardManagementStrategyEditPageDesfireStudio />} />,
});

const desfireStudioChipDesignerRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/chip-designer',
  component: () => <ProtectedLazyRoute component={<CardManagementChipDesignerPage />} />,
});

const desfireStudioCardEditorRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/card-editor',
  component: () => <ProtectedLazyRoute component={<CardManagementCardEditorPage />} />,
});

const desfireStudioCardEditorCreateRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/card-editor/new',
  component: () => <ProtectedLazyRoute component={<CardManagementPrintDesignCreatePage />} />,
});

const desfireStudioCardEditorEditRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/card-editor/$printDesignId/edit',
  component: () => <ProtectedLazyRoute component={<CardManagementPrintDesignEditPage />} />,
});

const desfireStudioChipDesignCreateRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/chip-designs/new',
  component: () => <ProtectedLazyRoute component={<CardManagementChipDesignCreatePage />} />,
});

const desfireStudioChipDesignEditRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/chip-designs/$chipDesignId/edit',
  component: () => <ProtectedLazyRoute component={<CardManagementChipDesignEditPageDesfireStudio />} />,
});

const desfireStudioTransformationCreateRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/transformations/new',
  component: () => <ProtectedLazyRoute component={<CardManagementTransformationCreatePage />} />,
});

const desfireStudioTransformationEditRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/transformations/$transformationId/edit',
  component: () => <ProtectedLazyRoute component={<CardManagementTransformationEditPageDesfireStudio />} />,
});

const desfireStudioSystemProviderCreateRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/system-providers/new',
  component: () => <ProtectedLazyRoute component={<CardManagementSystemProviderCreatePage />} />,
});

const desfireStudioPrintingRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/printing',
  component: () => <ProtectedLazyRoute component={<CardManagementPrintingPage />} />,
});

const desfireStudioPrintBatchCreateRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/printing/new',
  component: () => <ProtectedLazyRoute component={<CardManagementPrintBatchCreatePage />} />,
});

const desfireStudioPrintBatchDetailRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/printing/$batchId',
  component: () => <ProtectedLazyRoute component={<CardManagementPrintBatchDetailPageDesfireStudio />} />,
});

const desfireStudioEncoderCreateRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/printing/encoders/new',
  component: () => <ProtectedLazyRoute component={<CardManagementEncoderFormPage />} />,
});

const desfireStudioEncoderEditRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/printing/encoders/$encoderId/edit',
  component: () => <ProtectedLazyRoute component={<CardManagementEncoderFormPageDesfireStudio />} />,
});

const desfireStudioPrintRunDetailRoute = createRoute({
  getParentRoute: () => desfireStudioLayoutRoute,
  path: '/printing/runs/$runId',
  component: () => <ProtectedLazyRoute component={<CardManagementPrintRunDetailPageDesfireStudio />} />,
});

const receptionDeskWorkstationIndexRoute = createRoute({
  getParentRoute: () => receptionDeskWorkstationLayoutRoute,
  path: '/',
  component: () => <LazyRoute component={<ReceptionDeskWorkstationPage />} />,
});

const receptionDeskWorkstationSetupRoute = createRoute({
  getParentRoute: () => receptionDeskWorkstationLayoutRoute,
  path: '/setup',
  component: () => <ProtectedLazyRoute component={<ReceptionDeskWorkstationSetupPage />} />,
});

const receptionDeskWorkstationExpectedArrivalsRoute = createRoute({
  getParentRoute: () => receptionDeskWorkstationLayoutRoute,
  path: '/expected-arrivals',
  component: () => <ProtectedLazyRoute component={<ReceptionDeskExpectedArrivalsPage />} />,
});

const receptionDeskWorkstationArrivalsRoute = createRoute({
  getParentRoute: () => receptionDeskWorkstationLayoutRoute,
  path: '/arrivals',
  component: () => <ProtectedLazyRoute component={<ReceptionDeskArrivalsPage />} />,
});

const receptionDeskWorkstationHistoryRoute = createRoute({
  getParentRoute: () => receptionDeskWorkstationLayoutRoute,
  path: '/history',
  component: () => <ProtectedLazyRoute component={<ReceptionDeskHistoryPage />} />,
});

const visitorConfirmationRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/visitor-confirmation/$visitId/$invitationId',
  component: () => <LazyRoute component={<VisitorConfirmationPage />} />,
});

const receptionKioskIndexRoute = createRoute({
  getParentRoute: () => receptionKioskLayoutRoute,
  path: '/',
  component: () => <LazyRoute component={<ReceptionKioskPage />} />,
});

const receptionKioskSetupRoute = createRoute({
  getParentRoute: () => receptionKioskLayoutRoute,
  path: '/setup',
  component: () => <LazyRoute component={<ReceptionKioskSetupPage />} />,
});

const receptionKioskScanQrRoute = createRoute({
  getParentRoute: () => receptionKioskLayoutRoute,
  path: '/scan-qr',
  component: () => <LazyRoute component={<ReceptionKioskScanQrPage />} />,
});

const receptionKioskArrivalRoute = createRoute({
  getParentRoute: () => receptionKioskLayoutRoute,
  path: '/arrival',
  component: () => <LazyRoute component={<ReceptionKioskArrivalPage />} />,
});

const receptionKioskFaceScanRoute = createRoute({
  getParentRoute: () => receptionKioskLayoutRoute,
  path: '/scan-face',
  component: () => <LazyRoute component={<ReceptionKioskFaceScanPage />} />,
});

const receptionKioskDocumentScanRoute = createRoute({
  getParentRoute: () => receptionKioskLayoutRoute,
  path: '/scan-document',
  component: () => <LazyRoute component={<ReceptionKioskDocumentScanPage />} />,
});

const receptionKioskSuccessRoute = createRoute({
  getParentRoute: () => receptionKioskLayoutRoute,
  path: '/success',
  component: () => <LazyRoute component={<ReceptionKioskSuccessPage />} />,
});

const receptionKioskFailedRoute = createRoute({
  getParentRoute: () => receptionKioskLayoutRoute,
  path: '/failed',
  component: () => <LazyRoute component={<ReceptionKioskFailedPage />} />,
});

const receptionKioskNoRegistrationRoute = createRoute({
  getParentRoute: () => receptionKioskLayoutRoute,
  path: '/no-registration',
  component: () => <LazyRoute component={<ReceptionKioskNoRegistrationPage />} />,
});

const receptionKioskWrongLocationRoute = createRoute({
  getParentRoute: () => receptionKioskLayoutRoute,
  path: '/wrong-location',
  component: () => <LazyRoute component={<ReceptionKioskWrongLocationPage />} />,
});

const kioskIndexRoute = createRoute({
  getParentRoute: () => kioskLayoutRoute,
  path: '/',
  component: () => <LazyRoute component={<KioskPage />} />,
});

const kioskSetupRoute = createRoute({
  getParentRoute: () => kioskLayoutRoute,
  path: '/setup',
  component: () => <LazyRoute component={<KioskSetupPage />} />,
});

const routeTree = rootRoute.addChildren([
  mainLayoutRoute.addChildren([
    indexRoute,
    authCallbackRoute,
    employeeRoute,
    employeeRequestAccessRoute,
    employeeRequestDetailRoute,
    employeeContractorsRoute,
    employeeContractorCompanyCreateRoute,
    employeeContractorCompanyDetailRoute,
    employeeContractorCreateRoute,
    employeeContractorDetailRoute,
    employeeContractorJobCreateRoute,
    employeeContractorJobDetailRoute,
    employeeContractorAssignmentCreateRoute,
    employeeContractorAssignmentDetailRoute,
    employeeVisitorsRoute,
    employeeVisitCreateRoute,
    employeeVisitEditRoute,
    employeeVisitInvitationDetailRoute,
    managerRoute,
    managerMyTeamRoute,
    managerApprovalInboxRoute,
    managerApprovalInboxDetailRoute,
    managerTeamMemberDetailRoute,
    securityOfficerRoute,
    securityOfficerIdentitiesRoute,
    securityOfficerIdentityDetailRoute,
    securityOfficerIdentityRequestDetailRoute,
    integrationsRoute.addChildren([
      integrationsIndexRoute,
      integrationsMicrosoftGraphRoute,
      integrationsKeycloakRoute,
    ]),
    administrationRoute.addChildren([
      administrationIndexRoute,
      administrationSitesRoute,
      administrationClientsRoute,
      administrationAutomationRoute.addChildren([
        administrationAutomationIndexRoute,
        administrationAutomationWorkflowRoute,
        administrationAutomationWorkflowDefinitionsRoute,
        administrationAutomationWorkflowDefinitionEditorRoute,
        administrationAutomationWorkflowInstancesRoute,
        administrationAutomationWorkflowInstanceViewerRoute,
        administrationAutomationKioskRoute,
        administrationAutomationKioskEditRoute,
        administrationAutomationKioskProfileEditRoute,
      ]),
      administrationAccessModelRoute,
      administrationLmsRoute,
      administrationLmsCourseCreateRoute,
      administrationLmsCourseEditRoute,
      administrationLmsCourseLanguageCreateRoute,
      administrationLmsCourseLanguageEditRoute,
      administrationLmsEnrollmentCreateRoute,
      administrationLmsCourseRequirementCreateRoute,
      administrationLmsCourseRequirementEditRoute,
      administrationCredentialTypesRoute,
      administrationAccessControlRoute,
      administrationUserManagementRoute,
      administrationUserManagementUserCreateRoute,
      administrationUserManagementUserEditRoute,
      administrationUserManagementRoleCreateRoute,
      administrationUserManagementRoleEditRoute,
      administrationUserManagementGroupCreateRoute,
      administrationUserManagementGroupEditRoute,
      administrationNotificationsRoute,
      administrationCredentialTypeCreateRoute,
      administrationCredentialTypeEditRoute,
      administrationAccessItemCreateRoute,
      administrationAccessItemEditRoute,
      administrationAccessControlTargetEditRoute,
      administrationAccessControlSystemCreateRoute,
      administrationAccessControlSystemEditRoute,
      administrationPackageCreateRoute,
      administrationPackageEditRoute,
      administrationRequirementCreateRoute,
      administrationRequirementEditRoute,
      administrationCatalogueCreateRoute,
      administrationCatalogueEditRoute,
      administrationApprovalGroupCreateRoute,
      administrationApprovalGroupEditRoute,
      administrationMyOrganizationRoute,
      administrationContractorJobTypeCreateRoute,
      administrationContractorJobTypeEditRoute,
      administrationEmployeeCreateRoute,
      administrationEmployeeEditRoute,
      administrationOrganizationUnitCreateRoute,
      administrationOrganizationUnitEditRoute,
      administrationPersonaCreateRoute,
      administrationPersonaEditRoute,
      administrationSiteCreateRoute,
      administrationSiteEditRoute,
      administrationBuildingEditRoute,
      administrationRoomEditRoute,
      administrationHardwareAgentDetailRoute,
    ]),
    visitorConfirmationRoute,
  ]),
  receptionKioskLayoutRoute.addChildren([receptionKioskIndexRoute, receptionKioskSetupRoute, receptionKioskScanQrRoute, receptionKioskArrivalRoute, receptionKioskFaceScanRoute, receptionKioskDocumentScanRoute, receptionKioskSuccessRoute, receptionKioskFailedRoute, receptionKioskNoRegistrationRoute, receptionKioskWrongLocationRoute]),
  receptionDeskWorkstationLayoutRoute.addChildren([
    receptionDeskWorkstationIndexRoute,
    receptionDeskWorkstationSetupRoute,
    receptionDeskWorkstationExpectedArrivalsRoute,
    receptionDeskWorkstationArrivalsRoute,
    receptionDeskWorkstationHistoryRoute,
  ]),
  desfireStudioLayoutRoute.addChildren([
    desfireStudioIndexRoute,
    desfireStudioHardwareAgentsRoute,
    desfireStudioHardwareAgentDetailRoute,
    desfireStudioKeyManagementRoute,
    desfireStudioKeyGroupCreateRoute,
    desfireStudioKeyGroupEditRoute,
    desfireStudioStrategyCreateRoute,
    desfireStudioStrategyEditRoute,
    desfireStudioChipDesignerRoute,
    desfireStudioCardEditorRoute,
    desfireStudioCardEditorCreateRoute,
    desfireStudioCardEditorEditRoute,
    desfireStudioChipDesignCreateRoute,
    desfireStudioChipDesignEditRoute,
    desfireStudioTransformationCreateRoute,
    desfireStudioTransformationEditRoute,
    desfireStudioSystemProviderCreateRoute,
    desfireStudioPrintingRoute,
    desfireStudioPrintBatchCreateRoute,
    desfireStudioPrintBatchDetailRoute,
    desfireStudioEncoderCreateRoute,
    desfireStudioEncoderEditRoute,
    desfireStudioPrintRunDetailRoute,
  ]),
  kioskLayoutRoute.addChildren([kioskIndexRoute, kioskSetupRoute]),
]);

export function createAppRouter() {
  return createRouter({ routeTree });
}

export const router = createAppRouter();

export type AppRouter = typeof router;

declare module '@tanstack/react-router' {
  interface Register {
    router: AppRouter;
  }
}

function LazyRoute({ component }: { component: React.ReactNode }) {
  return <Suspense fallback={<RouteFallback />}>{component}</Suspense>;
}

function ProtectedLazyRoute({ component }: { component: React.ReactNode }) {
  return (
    <ProtectedRoute>
      <LazyRoute component={component} />
    </ProtectedRoute>
  );
}

function RouteFallback() {
  const { t } = useTranslation();

  return <div className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('shell.routeFallback')}</div>;
}
