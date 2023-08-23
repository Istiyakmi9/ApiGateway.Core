namespace ApiGateway.Core.Modal
{
    public class MasterDatabase
    {
        public string Server { set; get; }
        public int Port { set; get; }
        public string Database { set; get; }
        public string User_Id { set; get; }
        public string Password { set; get; }
        public int Connection_Timeout { set; get; }
        public int Connection_Lifetime { set; get; }
        public int Min_Pool_Size { set; get; }
        public int Max_Pool_Size { set; get; }
        public bool Pooling { set; get; }
    }
}
