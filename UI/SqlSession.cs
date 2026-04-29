using System.ComponentModel;
using KnifeSQLExtension.Core.Services.Database.Interfaces;

namespace KnifeSQLExtension.UI
{
    public class SqlSession
    {
        public event Action<bool> ConnectionStateChanged;
        private IDatabaseClient? _dbClient = null;
        
        private bool _isConnected;
        public bool IsConnected 
        { 
            get => _isConnected; 
            private set 
            {
                if (_isConnected == value) return;
                _isConnected = value;
                ConnectionStateChanged?.Invoke(_isConnected);
            }
        }

        public void ConnectDbClient(IDatabaseClient dbClient, Boolean isConnected = false)
        {
            _dbClient = dbClient;
            IsConnected = isConnected;
        }

        public IDatabaseClient? GetDbClient()
        {
            return _dbClient;
        }
        
    }
}