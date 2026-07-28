import { Navigate, Outlet, createRootRoute, createRoute, createRouter, redirect } from '@tanstack/react-router';
import { lazy, Suspense } from 'react';

import { AppLayout } from '@/shared/layout/app-layout';
import { ProtectedRoute } from '@/shared/auth/protected-route';
import EmployeeCreatePage from '@/features/administration/employee-create-page';
import EmployeeEditPage from '@/features/administration/employee-edit-page';
import AccessItemCreatePage from '@/features/administration/access-item-create-page';
import AccessItemEditPage from '@/features/administration/access-item-edit-page';
import AccessControlTargetEditPage from '@/features/administration/access-control-target-edit-page';
import AccessControlPage from '@/features/administration/access-control-page';
import AccessControlSystemCreatePage from '@/features/administration/access-control-system-create-page';
import AccessControlSystemEditPage from '@/features/administration/access-control-system-edit-page';
import AccessModelPage from '@/features/administration/access-model-page';
import NotificationsPage from '@/features/administration/notifications-page';
import ApprovalGroupCreatePage from '@/features/administration/approval-group-create-page';
import ApprovalGroupEditPage from '@/features/administration/approval-group-edit-page';
import CatalogueCreatePage from '@/features/administration/catalogue-create-page';
import CatalogueEditPage from '@/features/administration/catalogue-edit-page';
import PackageCreatePage from '@/features/administration/package-create-page';
import PackageEditPage from '@/features/administration/package-edit-page';
import MyOrganizationPage from '@/features/administration/my-organization-page';
import OrganizationUnitCreatePage from '@/features/administration/organization-unit-create-page';
import OrganizationUnitEditPage from '@/features/administration/organization-unit-edit-page';
import PersonaCreatePage from '@/features/administration/persona-create-page';
import PersonaEditPage from '@/features/administration/persona-edit-page';
import { PerspectiveHomePage } from '@/features/perspectives/perspective-home-page';
import { ReceptionKioskLayout } from '@/features/reception-kiosk/layout/reception-kiosk-layout';

const AccessPage = lazy(() => import('@/features/access/access-page'));
const AuditPage = lazy(() => import('@/features/audit/audit-page'));
const AuthCallbackPage = lazy(() => import('@/features/auth/auth-callback-page'));
const AutomationWorkflowDefinitionEditorPage = lazy(() => import('@/features/automation/workflow-definition-editor-page'));
const AutomationWorkflowInstanceViewerPage = lazy(() => import('@/features/automation/workflow-instance-viewer-page'));
const AutomationWorkflowPage = lazy(() => import('@/features/automation/workflow-page'));
const AutomationKioskPage = lazy(() => import('@/features/automation/kiosk-admin-page'));
const AutomationKioskEditPage = lazy(() => import('@/features/automation/kiosk-edit-page'));
const AutomationKioskProfileEditPage = lazy(() => import('@/features/automation/kiosk-profile-edit-page'));
const CardManagementChipDesignCreatePage = lazy(() => import('@/features/card-management/chip-design-create-page'));
const CardManagementChipDesignEditPage = lazy(() => import('@/features/card-management/chip-design-form-page'));
const CardManagementChipDesignerPage = lazy(() => import('@/features/card-management/chip-designer-page'));
const CardManagementKeyGroupCreatePage = lazy(() => import('@/features/card-management/key-group-create-page'));
const CardManagementKeyGroupEditPage = lazy(() => import('@/features/card-management/key-group-form-page'));
const CardManagementKeyManagementPage = lazy(() => import('@/features/card-management/key-management-page'));
const CardManagementPrintBatchCreatePage = lazy(() => import('@/features/card-management/print-batch-create-page'));
const CardManagementPrintBatchDetailPage = lazy(() => import('@/features/card-management/print-batch-detail-page'));
const CardManagementEncoderFormPage = lazy(() => import('@/features/card-management/encoder-form-page'));
const CardManagementPrintRunDetailPage = lazy(() => import('@/features/card-management/print-run-detail-page'));
const CardManagementPrintingPage = lazy(() => import('@/features/card-management/printing-page'));
const CardManagementStrategyCreatePage = lazy(() => import('@/features/card-management/diversification-strategy-create-page'));
const CardManagementStrategyEditPage = lazy(() => import('@/features/card-management/diversification-strategy-form-page'));
const CardManagementSystemProviderCreatePage = lazy(() => import('@/features/card-management/system-provider-create-page'));
const CardManagementTransformationCreatePage = lazy(() => import('@/features/card-management/transformation-create-page'));
const CardManagementTransformationEditPage = lazy(() => import('@/features/card-management/transformation-form-page'));
const CredentialsPage = lazy(() => import('@/features/credentials/credentials-page'));
const FacilityHardwareAgentDetailPage = lazy(() => import('@/features/facility/hardware-agent-detail-page'));
const FacilityBuildingEditPage = lazy(() => import('@/features/facility/building-edit-page'));
const FacilityHardwarePage = lazy(() => import('@/features/facility/hardware-page'));
const FacilityRoomEditPage = lazy(() => import('@/features/facility/room-edit-page'));
const FacilitySiteCreatePage = lazy(() => import('@/features/facility/site-create-page'));
const FacilitySiteEditPage = lazy(() => import('@/features/facility/site-edit-page'));
const FacilityLocationsPage = lazy(() => import('@/features/facility/locations-page'));
const HomePage = lazy(() => import('@/features/home/home-page'));
const EmployeeRequestAccessPage = lazy(() => import('@/features/perspectives/employee-request-access-page'));
const EmployeeRequestDetailPage = lazy(() => import('@/features/perspectives/employee-request-detail-page'));
const ManagerApprovalInboxPage = lazy(() => import('@/features/perspectives/manager-approval-inbox-page'));
const IdentityDetailPage = lazy(() => import('@/features/identities/identity-detail-page'));
const IdentitiesPage = lazy(() => import('@/features/identities/identities-page'));
const KioskPage = lazy(() => import('@/features/kiosk/kiosk-page'));
const KioskSetupPage = lazy(() => import('@/features/kiosk/kiosk-setup-page'));
const OrganizationsPage = lazy(() => import('@/features/organizations/organizations-page'));
const ReceptionDeskPage = lazy(() => import('@/features/reception-desk/reception-desk-page'));
const ReceptionKioskArrivalPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-arrival-page'));
const ReceptionKioskDocumentScanPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-document-scan-page'));
const ReceptionKioskFaceScanPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-face-scan-page'));
const ReceptionKioskFailedPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-failed-page'));
const ReceptionKioskNoRegistrationPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-no-registration-page'));
const ReceptionKioskPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-page'));
const ReceptionKioskScanQrPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-scan-qr-page'));
const ReceptionKioskSetupPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-setup-page'));
const ReceptionKioskSuccessPage = lazy(() => import('@/features/reception-kiosk/reception-kiosk-success-page'));
const VisitorsManagementLayout = lazy(() => import('@/features/visitors-management/visitors-management-layout'));
const VisitorConfirmationPage = lazy(() => import('@/features/visitor-confirmation/visitor-confirmation-page'));
const VisitorReportingPage = lazy(() => import('@/features/visitors-management/reporting-page'));
const VisitCreatePage = lazy(() => import('@/features/visitors-management/visit-create-page'));
const VisitEditPage = lazy(() => import('@/features/visitors-management/visit-edit-page'));
const VisitorsPage = lazy(() => import('@/features/visitors-management/visitors-page'));
const VisitsPage = lazy(() => import('@/features/visitors-management/visits-page'));

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

const identitiesRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/identities',
  component: () => <ProtectedLazyRoute component={<IdentitiesPage />} />,
});

const accessRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/access',
  component: () => <ProtectedLazyRoute component={<AccessPage />} />,
});

const credentialsRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/credentials',
  component: () => <ProtectedLazyRoute component={<CredentialsPage />} />,
});

const organizationsRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/organizations',
  component: () => <ProtectedLazyRoute component={<OrganizationsPage />} />,
});

const auditRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/audit',
  component: () => <ProtectedLazyRoute component={<AuditPage />} />,
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

const administrationAccessModelRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model',
  component: () => <LazyRoute component={<AccessModelPage />} />,
});

const administrationAccessControlRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-control',
  component: () => <LazyRoute component={<AccessControlPage />} />,
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

const administrationPackageEditRoute = createRoute({
  getParentRoute: () => administrationRoute,
  path: '/access-model/packages/$packageId/edit',
  component: () => <LazyRoute component={<PackageEditPage />} />,
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

const automationRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/automation',
  component: () => (
    <ProtectedRoute>
      <Outlet />
    </ProtectedRoute>
  ),
});

const workflowsAliasRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/workflows',
  component: () => (
    <ProtectedRoute>
      <Outlet />
    </ProtectedRoute>
  ),
});

const automationIndexRoute = createRoute({
  getParentRoute: () => automationRoute,
  path: '/',
  component: () => <Navigate to="/old/automation/workflow" search={{ tab: 'definitions' } as never} />,
});

const automationWorkflowRoute = createRoute({
  getParentRoute: () => automationRoute,
  path: '/workflow',
  component: () => <LazyRoute component={<AutomationWorkflowPage />} />,
});

const automationWorkflowDefinitionsRoute = createRoute({
  getParentRoute: () => automationRoute,
  path: '/workflow-definitions',
  component: () => <Navigate to="/old/automation/workflow" search={{ tab: 'definitions' } as never} />,
});

const automationWorkflowDefinitionEditorRoute = createRoute({
  getParentRoute: () => automationRoute,
  path: '/workflow-definitions/$definitionId/edit',
  component: () => <LazyRoute component={<AutomationWorkflowDefinitionEditorPage />} />,
});

const automationWorkflowInstancesRoute = createRoute({
  getParentRoute: () => automationRoute,
  path: '/workflow-instances',
  component: () => <Navigate to="/old/automation/workflow" search={{ tab: 'history' } as never} />,
});

const automationWorkflowInstanceViewerRoute = createRoute({
  getParentRoute: () => automationRoute,
  path: '/workflow-instances/$instanceId',
  component: () => <LazyRoute component={<AutomationWorkflowInstanceViewerPage />} />,
});

const workflowDefinitionEditorAliasRoute = createRoute({
  getParentRoute: () => workflowsAliasRoute,
  path: '/definitions/$definitionId/edit',
  beforeLoad: ({ params }) => {
    throw redirect({
      to: '/old/automation/workflow-definitions/$definitionId/edit',
      params: { definitionId: params.definitionId },
      replace: true,
    });
  },
});

const workflowInstanceViewerAliasRoute = createRoute({
  getParentRoute: () => workflowsAliasRoute,
  path: '/instances/$instanceId/view',
  beforeLoad: ({ params }) => {
    throw redirect({
      to: '/old/automation/workflow-instances/$instanceId',
      params: { instanceId: params.instanceId },
      replace: true,
    });
  },
});

const automationKioskRoute = createRoute({
  getParentRoute: () => automationRoute,
  path: '/kiosk',
  component: () => <LazyRoute component={<AutomationKioskPage />} />,
});

const automationKioskEditRoute = createRoute({
  getParentRoute: () => automationRoute,
  path: '/kiosk/$kioskId/edit',
  component: () => <LazyRoute component={<AutomationKioskEditPage />} />,
});

const automationKioskProfileEditRoute = createRoute({
  getParentRoute: () => automationRoute,
  path: '/kiosk/profiles/$profileId/edit',
  component: () => <LazyRoute component={<AutomationKioskProfileEditPage />} />,
});

const cardManagementRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/card-management',
  component: () => (
    <ProtectedRoute>
      <Outlet />
    </ProtectedRoute>
  ),
});

const cardManagementIndexRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/',
  component: () => <Navigate to="/old/card-management/key-management" />,
});

const cardManagementKeyManagementRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/key-management',
  component: () => <LazyRoute component={<CardManagementKeyManagementPage />} />,
});

const cardManagementChipDesignerRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/chip-designer',
  component: () => <LazyRoute component={<CardManagementChipDesignerPage />} />,
});

const cardManagementChipDesignCreateRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/chip-designs/new',
  component: () => <LazyRoute component={<CardManagementChipDesignCreatePage />} />,
});

const cardManagementChipDesignEditRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/chip-designs/$chipDesignId/edit',
  component: () => <LazyRoute component={<CardManagementChipDesignEditPage />} />,
});

const cardManagementKeyGroupCreateRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/key-groups/new',
  component: () => <LazyRoute component={<CardManagementKeyGroupCreatePage />} />,
});

const cardManagementKeyGroupEditRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/key-groups/$keyGroupId/edit',
  component: () => <LazyRoute component={<CardManagementKeyGroupEditPage />} />,
});

const cardManagementStrategyCreateRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/diversification-strategies/new',
  component: () => <LazyRoute component={<CardManagementStrategyCreatePage />} />,
});

const cardManagementStrategyEditRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/diversification-strategies/$strategyId/edit',
  component: () => <LazyRoute component={<CardManagementStrategyEditPage />} />,
});

const cardManagementTransformationCreateRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/transformations/new',
  component: () => <LazyRoute component={<CardManagementTransformationCreatePage />} />,
});

const cardManagementTransformationEditRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/transformations/$transformationId/edit',
  component: () => <LazyRoute component={<CardManagementTransformationEditPage />} />,
});

const cardManagementSystemProviderCreateRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/system-providers/new',
  component: () => <LazyRoute component={<CardManagementSystemProviderCreatePage />} />,
});

const cardManagementPrintingRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/printing',
  component: () => <LazyRoute component={<CardManagementPrintingPage />} />,
});

const cardManagementPrintBatchCreateRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/printing/new',
  component: () => <LazyRoute component={<CardManagementPrintBatchCreatePage />} />,
});

const cardManagementPrintBatchDetailRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/printing/$batchId',
  component: () => <LazyRoute component={<CardManagementPrintBatchDetailPage />} />,
});

const cardManagementEncoderCreateRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/printing/encoders/new',
  component: () => <LazyRoute component={<CardManagementEncoderFormPage />} />,
});

const cardManagementEncoderEditRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/printing/encoders/$encoderId/edit',
  component: () => <LazyRoute component={<CardManagementEncoderFormPage />} />,
});

const cardManagementPrintRunDetailRoute = createRoute({
  getParentRoute: () => cardManagementRoute,
  path: '/printing/runs/$runId',
  component: () => <LazyRoute component={<CardManagementPrintRunDetailPage />} />,
});

const receptionDeskRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/reception-desk',
  component: () => <ProtectedLazyRoute component={<ReceptionDeskPage />} />,
});

const visitorConfirmationRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/visitor-confirmation/$visitId/$invitationId',
  component: () => <LazyRoute component={<VisitorConfirmationPage />} />,
});

const facilityRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/facility',
  component: () => (
    <ProtectedRoute>
      <Outlet />
    </ProtectedRoute>
  ),
});

const facilityHardwareRoute = createRoute({
  getParentRoute: () => facilityRoute,
  path: '/hardware',
  component: () => <LazyRoute component={<FacilityHardwarePage />} />,
});

const facilityHardwareAgentDetailRoute = createRoute({
  getParentRoute: () => facilityRoute,
  path: '/hardware/$agentId',
  component: () => <LazyRoute component={<FacilityHardwareAgentDetailPage />} />,
});

const visitorsManagementRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/old/visitors-management',
  component: () => <ProtectedLazyRoute component={<VisitorsManagementLayout />} />,
});

const visitsIndexRoute = createRoute({
  getParentRoute: () => visitorsManagementRoute,
  path: '/',
  component: () => <LazyRoute component={<VisitsPage />} />,
});

const visitsRoute = createRoute({
  getParentRoute: () => visitorsManagementRoute,
  path: '/visits',
  component: () => <LazyRoute component={<VisitsPage />} />,
});

const visitCreateRoute = createRoute({
  getParentRoute: () => visitorsManagementRoute,
  path: '/visits/new',
  component: () => <LazyRoute component={<VisitCreatePage />} />,
});

const visitorsRoute = createRoute({
  getParentRoute: () => visitorsManagementRoute,
  path: '/visitors',
  component: () => <LazyRoute component={<VisitorsPage />} />,
});

const visitEditRoute = createRoute({
  getParentRoute: () => visitorsManagementRoute,
  path: '/visits/$visitId/edit',
  component: () => <LazyRoute component={<VisitEditPage />} />,
});

const visitorReportingRoute = createRoute({
  getParentRoute: () => visitorsManagementRoute,
  path: '/reporting',
  component: () => <LazyRoute component={<VisitorReportingPage />} />,
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
    identitiesRoute,
    accessRoute,
    credentialsRoute,
    organizationsRoute,
    auditRoute,
    employeeRoute,
    employeeRequestAccessRoute,
    employeeRequestDetailRoute,
    managerRoute,
    managerApprovalInboxRoute,
    managerApprovalInboxDetailRoute,
    securityOfficerRoute,
    securityOfficerIdentitiesRoute,
    securityOfficerIdentityDetailRoute,
    securityOfficerIdentityRequestDetailRoute,
    administrationRoute.addChildren([
      administrationIndexRoute,
      administrationSitesRoute,
      administrationAccessModelRoute,
      administrationAccessControlRoute,
      administrationNotificationsRoute,
      administrationAccessItemCreateRoute,
      administrationAccessItemEditRoute,
      administrationAccessControlTargetEditRoute,
      administrationAccessControlSystemCreateRoute,
      administrationAccessControlSystemEditRoute,
      administrationPackageCreateRoute,
      administrationPackageEditRoute,
      administrationCatalogueCreateRoute,
      administrationCatalogueEditRoute,
      administrationApprovalGroupCreateRoute,
      administrationApprovalGroupEditRoute,
      administrationMyOrganizationRoute,
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
    ]),
    automationRoute.addChildren([
      automationIndexRoute,
      automationWorkflowRoute,
      automationWorkflowDefinitionsRoute,
      automationWorkflowDefinitionEditorRoute,
      automationWorkflowInstancesRoute,
      automationWorkflowInstanceViewerRoute,
      automationKioskRoute,
      automationKioskEditRoute,
      automationKioskProfileEditRoute,
    ]),
    workflowsAliasRoute.addChildren([
      workflowDefinitionEditorAliasRoute,
      workflowInstanceViewerAliasRoute,
    ]),
    cardManagementRoute.addChildren([
      cardManagementIndexRoute,
      cardManagementKeyManagementRoute,
      cardManagementChipDesignerRoute,
      cardManagementChipDesignCreateRoute,
      cardManagementChipDesignEditRoute,
      cardManagementKeyGroupCreateRoute,
      cardManagementKeyGroupEditRoute,
      cardManagementStrategyCreateRoute,
      cardManagementStrategyEditRoute,
      cardManagementTransformationCreateRoute,
      cardManagementTransformationEditRoute,
      cardManagementSystemProviderCreateRoute,
      cardManagementPrintingRoute,
      cardManagementPrintBatchCreateRoute,
      cardManagementPrintBatchDetailRoute,
      cardManagementEncoderCreateRoute,
      cardManagementEncoderEditRoute,
      cardManagementPrintRunDetailRoute,
    ]),
    receptionDeskRoute,
    visitorConfirmationRoute,
    facilityRoute.addChildren([facilityHardwareRoute, facilityHardwareAgentDetailRoute]),
    visitorsManagementRoute.addChildren([visitsIndexRoute, visitsRoute, visitCreateRoute, visitEditRoute, visitorsRoute, visitorReportingRoute]),
  ]),
  receptionKioskLayoutRoute.addChildren([receptionKioskIndexRoute, receptionKioskSetupRoute, receptionKioskScanQrRoute, receptionKioskArrivalRoute, receptionKioskFaceScanRoute, receptionKioskDocumentScanRoute, receptionKioskSuccessRoute, receptionKioskFailedRoute, receptionKioskNoRegistrationRoute]),
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
  return <div className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">Loading...</div>;
}
