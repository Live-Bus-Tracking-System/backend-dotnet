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
            public const string Read = "route:read";
        }

        public static class Vehicles
        {
            public const string Create     = "vehicle:create";
            public const string Read       = "vehicle:read";
            public const string Update     = "vehicle:update";
            public const string Delete     = "vehicle:delete";
            public const string Deactivate = "vehicle:deactivate";
        }

        public static class Permits
        {
            public const string Request = "permit:request";
            public const string Approve = "permit:approve";
            public const string Read = "permit:read";
        }

        public static class Orgs
        {
            public const string Read = "org:read";
            public const string ReadAll = "org:read:all";
            public const string Update = "org:update";
            public const string Delete = "org:delete";
            public const string Activate = "org:activate";
            public const string Suspend = "org:suspend";
        }

        // full set per role — used by the seeder
        public static readonly IReadOnlyList<string> SuperAdminPermissions =
        [
            Stops.Create,
            Routes.Create, Routes.Read,
            Vehicles.Create, Vehicles.Read, Vehicles.Update, Vehicles.Delete, Vehicles.Deactivate,
            Permits.Request, Permits.Approve, Permits.Read,
            Orgs.Read, Orgs.ReadAll, Orgs.Update, Orgs.Delete, Orgs.Activate, Orgs.Suspend
        ];

        public static readonly IReadOnlyList<string> TransitAuthorityAdminPermissions =
        [
            Stops.Create,
            Routes.Create, Routes.Read,
            Vehicles.Read,
            Permits.Approve, Permits.Read,
            Orgs.Read, Orgs.Update
        ];

        public static readonly IReadOnlyList<string> OrgAdminPermissions =
        [
            Routes.Create, Routes.Read,
            Vehicles.Create, Vehicles.Read, Vehicles.Update, Vehicles.Delete, Vehicles.Deactivate,
            Permits.Request, Permits.Read,
            Orgs.Read, Orgs.Update, Orgs.Delete
        ];

        public static readonly IReadOnlyList<string> OrgStaffPermissions =
        [
            Routes.Read,
            Vehicles.Read, Vehicles.Update,
            Permits.Read
        ];

        public static readonly IReadOnlyList<string> PassengerPermissions =
        [
            Routes.Read,
            Vehicles.Read
        ];
    }
}
