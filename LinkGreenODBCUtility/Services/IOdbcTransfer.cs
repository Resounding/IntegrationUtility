﻿using System.ComponentModel;

namespace LinkGreenODBCUtility
{
    interface IOdbcTransfer
    {
        bool Empty();

        void SaveTableMapping(string dsnName, string tableName);

        bool Publish(BackgroundWorker bw);
    }
}
