using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quiz.Support
{
    class DataBaseWorker
    {
        private string _connectionString;

        public void Init() {
            _connectionString = @"Data Source=\;Initial Catalog=C:";
        }
    }
}
