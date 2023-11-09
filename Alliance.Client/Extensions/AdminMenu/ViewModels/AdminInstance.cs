using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AdminMenu.ViewModels
{
    public sealed class AdminInstance
    {
        private AdminInstance() { }

        private static AdminVM _instance;
        private static MBBindingList<ServerMessageVM> _serverMessage = new MBBindingList<ServerMessageVM>();

        public static AdminVM GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AdminVM();
            }
            return _instance;
        }

        public static void SetInstance(AdminVM model)
        {
            _instance = model;

            if (_serverMessage.Count > 0)
            {
                _instance.ServerMessage = _serverMessage;
            }
        }

        public static void UpdateServerMessage(ServerMessageVM serverMessage)
        {
            _serverMessage.Add(serverMessage);
            if (_instance != null)
            {
                _instance.ServerMessage = _serverMessage;
            }
        }
    }
}
