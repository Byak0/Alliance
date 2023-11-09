namespace Alliance.Common.Core.Security.Models
{
    /// <summary>
    /// Singleton class containing player roles.
    /// </summary>
    public class Roles : DefaultRoles
    {
        private static Roles instance = new Roles();

        public static Roles Instance
        {
            get
            {
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        static Roles()
        {
        }
    }
}
