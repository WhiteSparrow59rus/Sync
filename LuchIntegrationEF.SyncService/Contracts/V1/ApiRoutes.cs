namespace LuchIntegrationEF.SyncService.Contracts.V1
{
    public class ApiRoutes
    {
        public const string Root = "api";
        public const string Version = "v1";
        public const string Base = Root + "/" + Version;

        public static class Synchronize
        {
            public const string Sync = Base + "/sync";
        }

        public static class System
        {
            public const string Luch = Synchronize.Sync + "/luch";
            public const string Milur = Synchronize.Sync + "/milur";
        }

        public static class ActionLuch
        {
            public const string Commands = System.Luch + "/commands";
            public const string Dictionary = System.Luch + "/dictionary";
            public const string All = System.Luch + "/all";
        }

        public static class ActionMilur
        {
            public const string All = System.Milur + "/all";
        }

        public static class Consumption
        {
            public const string Refresh = Base + "/refresh";
        }
    }
}
