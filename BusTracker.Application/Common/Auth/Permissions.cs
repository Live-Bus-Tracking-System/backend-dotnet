namespace BusTracker.Application.Common.Auth
{
    public static class Permissions
    {
        public static class Stops
        {
            public const string Create = "stop:create";
        }

        public static class Routes
        {
            public const string Create = "route:create";
            public const string Read   = "route:read";
        }

        public static class Vehicles
        {
            public const string Create = "vehicle:create";
            public const string Manage = "vehicle:manage";
            public const string Read   = "vehicle:read";
        }

        public static class Permits
        {
            public const string Request = "permit:request";
            public const string Approve = "permit:approve";
            public const string Read    = "permit:read";
        }

        public static class Orgs
        {
            public const string ManageOwn = "org:manage:own";
            public const string ManageAll = "org:manage:all";
        }

        // full set per role — used by the seeder
        public static readonly IReadOnlyList<string> SuperAdminPermissions =
        [
            Stops.Create,
            Routes.Create, Routes.Read,
            Vehicles.Create, Vehicles.Manage, Vehicles.Read,
            Permits.Request, Permits.Approve, Permits.Read,
            Orgs.ManageOwn, Orgs.ManageAll
        ];

        public static readonly IReadOnlyList<string> TransitAuthorityAdminPermissions =
        [
            Stops.Create,
            Routes.Create, Routes.Read,
            Vehicles.Read,
            Permits.Approve, Permits.Read,
            Orgs.ManageOwn
        ];

        public static readonly IReadOnlyList<string> OrgAdminPermissions =
        [
            Routes.Create, Routes.Read,
            Vehicles.Create, Vehicles.Manage, Vehicles.Read,
            Permits.Request, Permits.Read,
            Orgs.ManageOwn
        ];

        public static readonly IReadOnlyList<string> OrgStaffPermissions =
        [
            Routes.Read,
            Vehicles.Manage, Vehicles.Read,
            Permits.Read
        ];

        public static readonly IReadOnlyList<string> PassengerPermissions =
        [
            Routes.Read,
            Vehicles.Read
        ];
    }
}
