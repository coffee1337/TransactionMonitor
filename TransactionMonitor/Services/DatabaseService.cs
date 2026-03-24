using ABI.Windows.ApplicationModel.Activation;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using TransactionMonitor.Models;

namespace TransactionMonitor.Services
{
    public class DatabaseService
    {
        private readonly String _connectionString =
            "Server=USER\\SQLEXPRESS;Database=TransactionMonitoring;Trusted_Connection=True;TrustServerCertificate=True;";

        public List<Client> GetClients()
        {
            var clients = new List<Client>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("SELECT * FROM Clients ORDER BY RegistrationDate DESC", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                clients.Add(new Client
                {
                    ClientID = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Phone = reader.GetString(2),
                    Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    PassportSeries = reader.GetString(4).Trim(),
                    PassportNumber = reader.GetString(5).Trim(),
                    DateOfBirth = reader.GetDateTime(6),
                    RegistrationDate = reader.GetDateTime(7),
                    IsBlocked = reader.GetBoolean(8),
                    ScoringScore = reader.GetDouble(9)
                });
            }
            return clients;
        }

        public List<Transaction> GetTransactions()
        {
            var transactions = new List<Transaction>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = @"
                SELECT
                    t.TransactionID, t.SenderAccountID, t.ReceiverAccountID,
                    t.CounterpartyID, t.Amount, t.TransactionDate,
                    t.TransactionType, t.Status, t.Description, t.IPAddress,
                    c.FullName as SenderName,
                    cp.Name as CounterpartyName,
                    rs.RiskLevel, rs.RiskScore
                FROM Transactions t
                LEFT JOIN Accounts a ON a.AccountID = t.SenderAccountID
                LEFT JOIN Clients c ON c.ClientID = a.ClientID
                LEFT JOIN Counterparties cp ON cp.CounterpartyID = t.CounterpartyID
                LEFT JOIN RiskScores rs ON rs.TransactionID = t.TransactionID
                ORDER BY t.TransactionDate DESC";

            var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                transactions.Add(new Transaction
                {
                    TransactionID = reader.GetInt32(0),
                    SenderAccountID = reader.GetInt32(1),
                    ReceiverAcooundID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    CounterpartyID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Amount = reader.GetDecimal(4),
                    TransactionDate = reader.GetDateTime(5),
                    TransactionType = reader.GetString(6),
                    Status = reader.GetString(7),
                    Description = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    IPAddress = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    SenderName = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    CounterpartyName = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    RiskLevel = reader.IsDBNull(12) ? "-" : reader.GetString(12),
                    RiskScore = reader.IsDBNull(13) ? null : reader.GetDouble(13)
                });
            }
            return transactions;
        }

        public DashboardStats GetDashboardStats()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var stats = new DashboardStats();

            stats.TotalClients = (int)new SqlCommand("SELECT COUNT(*) FROM Clients", connection).ExecuteScalar();
            stats.BlockedClients = (int)new SqlCommand("SELECT COUNT(*) FROM Clients WHERE IsBlocked = 1", connection).ExecuteScalar();
            stats.TotalTransactions = (int)new SqlCommand("SELECT COUNT(*) FROM Transactions", connection).ExecuteScalar();
            stats.FraudCount = (int)new SqlCommand("SELECT COUNT(*) FROM RiskScores WHERE IsFraud = 1", connection).ExecuteScalar();
            stats.TotalAccounts = (int)new SqlCommand("SELECT COUNT(*) FROM Accounts", connection).ExecuteScalar();
            stats.ActiveAccounts = (int)new SqlCommand("SELECT COUNT(*) FROM Accounts WHERE Status = 'Active'", connection).ExecuteScalar();
            stats.FrozenAccounts = (int)new SqlCommand("SELECT COUNT(*) FROM Accounts WHERE Status = 'Frozen'", connection).ExecuteScalar();
            stats.TotalCounterparties = (int)new SqlCommand("SELECT COUNT(*) FROM Counterparties", connection).ExecuteScalar();
            stats.BlacklistedCounterparties = (int)new SqlCommand("SELECT COUNT(*) FROM Counterparties WHERE IsBlacklisted = 1", connection).ExecuteScalar();

            var sumCmd = new SqlCommand("SELECT ISNULL(SUM(Amount), 0) FROM Transactions", connection);
            stats.TotalAmount = Convert.ToDecimal(sumCmd.ExecuteScalar());

            var avgCmd = new SqlCommand("SELECT ISNULL(AVG(RiskScore), 0) FROM RiskScores", connection);
            stats.AvgRiskScore = Convert.ToDouble(avgCmd.ExecuteScalar());

            var highRiskCmd = new SqlCommand("SELECT COUNT(*) FROM RiskScores WHERE RiskLevel IN ('High','Critical') AND ReviewedByAnalyst = 0", connection);
            stats.UnreviewedHighRisk = (int)highRiskCmd.ExecuteScalar();

            return stats;
        }

        public List<Counterparty> GetCounterparties()
        {
            var list = new List<Counterparty>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(
                "SELECT CounterpartyID, Name, TaxID, ActivityCategory, RiskLevel, IsBlacklisted, CountryOfRegistry, AddedDate FROM Counterparties ORDER BY AddedDate DESC",
                connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new Counterparty
                {
                    CounterpartyID = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    TaxID = reader.GetString(2),
                    ActivityCategory = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    RiskLevel = reader.GetString(4),
                    IsBlacklisted = reader.GetBoolean(5),
                    CountryOfRegistry = reader.GetString(6),
                    AddedDate = reader.GetDateTime(7)
                });
            }
            return list;
        }

        public List<RiskScore> GetRiskScores()
        {
            var list = new List<RiskScore>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = @"
        SELECT rs.ScoreID, rs.TransactionID, c.FullName,
               rs.RiskScore, rs.RiskLevel, rs.ModelVersion,
               rs.ScoredAt, rs.IsFraud, rs.ReviewedByAnalyst, rs.AnalystComment
        FROM RiskScores rs
        LEFT JOIN Transactions t ON t.TransactionID = rs.TransactionID
        LEFT JOIN Accounts a ON a.AccountID = t.SenderAccountID
        LEFT JOIN Clients c ON c.ClientID = a.ClientID
        ORDER BY rs.ScoredAt DESC";

            var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new RiskScore
                {
                    ScoreID = reader.GetInt32(0),
                    TransactionID = reader.GetInt32(1),
                    SenderName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Score = reader.GetDouble(3),
                    RiskLevel = reader.GetString(4),
                    ModelVersion = reader.GetString(5),
                    ScoredAt = reader.GetDateTime(6),
                    IsFraud = reader.GetBoolean(7),
                    ReviewedByAnalyst = reader.GetBoolean(8),
                    AnalystComment = reader.IsDBNull(9) ? "" : reader.GetString(9)
                });
            }
            return list;
        }
        public (List<Account> accounts, List<Transaction> transactions) GetClientDetails(int clientId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // Счета клиента
            var accounts = new List<Account>();
            var accCmd = new SqlCommand(
                "SELECT AccountID, AccountNumber, Balance, Currency, Status, AccountType FROM Accounts WHERE ClientID = @id",
                connection);
            accCmd.Parameters.AddWithValue("@id", clientId);
            using (var reader = accCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    accounts.Add(new Account
                    {
                        AccountID = reader.GetInt32(0),
                        AccountNumber = reader.GetString(1),
                        Balance = reader.GetDecimal(2),
                        Currency = reader.GetString(3).Trim(),
                        Status = reader.GetString(4),
                        AccountType = reader.GetString(5)
                    });
                }
            }

            // Последние транзакции клиента
            var transactions = new List<Transaction>();
            var txCmd = new SqlCommand(@"
                SELECT TOP 10 t.TransactionID, t.Amount, t.TransactionDate,
                       t.TransactionType, t.Status, rs.RiskLevel
                FROM Transactions t
                JOIN Accounts a ON a.AccountID = t.SenderAccountID
                LEFT JOIN RiskScores rs ON rs.TransactionID = t.TransactionID
                WHERE a.ClientID = @id
                ORDER BY t.TransactionDate DESC", connection);
            txCmd.Parameters.AddWithValue("@id", clientId);
            using (var reader = txCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    transactions.Add(new Transaction
                    {
                        TransactionID = reader.GetInt32(0),
                        Amount = reader.GetDecimal(1),
                        TransactionDate = reader.GetDateTime(2),
                        TransactionType = reader.GetString(3),
                        Status = reader.GetString(4),
                        RiskLevel = reader.IsDBNull(5) ? "—" : reader.GetString(5)
                    });
                }
            }

            return (accounts, transactions);
        }
        public int CreateTransaction(int senderAccountId, int? receiverAccountId,
    int? counterpartyId, decimal amount, string transactionType,
    string description, string ipAddress)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // Создаём транзакцию
            var cmd = new SqlCommand(@"
        INSERT INTO Transactions
            (SenderAccountID, ReceiverAccountID, CounterpartyID,
             Amount, TransactionDate, TransactionType, Status, Description, IPAddress)
        VALUES
            (@sender, @receiver, @counterparty,
             @amount, GETDATE(), @type, 'Completed', @desc, @ip);
        SELECT SCOPE_IDENTITY();", connection);

            cmd.Parameters.AddWithValue("@sender", senderAccountId);
            cmd.Parameters.AddWithValue("@receiver", (object?)receiverAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@counterparty", (object?)counterpartyId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@type", transactionType);
            cmd.Parameters.AddWithValue("@desc", description);
            cmd.Parameters.AddWithValue("@ip", ipAddress);

            var newId = Convert.ToInt32(cmd.ExecuteScalar());

            // Считаем риск-скор
            var calculator = new RiskCalculator();
            var input = new RiskInput
            {
                Amount = amount,
                TransactionHour = DateTime.Now.Hour
            };
            var risk = calculator.Calculate(input);

            // Сохраняем риск-скор
            var riskCmd = new SqlCommand(@"
        INSERT INTO RiskScores
            (TransactionID, RiskScore, RiskLevel, ModelVersion, ScoredAt, IsFraud)
        VALUES
            (@txId, @score, @level, 'RuleEngine_v1.0', GETDATE(), 0)",
                connection);

            riskCmd.Parameters.AddWithValue("@txId", newId);
            riskCmd.Parameters.AddWithValue("@score", risk.Score);
            riskCmd.Parameters.AddWithValue("@level", risk.Level);
            riskCmd.ExecuteNonQuery();

            return newId;
        }

        public List<Account> GetAllAccounts()
        {
            var list = new List<Account>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand(@"
                SELECT a.AccountID, a.AccountNumber, a.Balance, a.Currency,
                       a.Status, a.AccountType, c.FullName
                FROM Accounts a
                JOIN Clients c ON c.ClientID = a.ClientID
                WHERE a.Status = 'Active'
                ORDER BY c.FullName", connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Account
                {
                    AccountID = reader.GetInt32(0),
                    AccountNumber = reader.GetString(1),
                    Balance = reader.GetDecimal(2),
                    Currency = reader.GetString(3).Trim(),
                    Status = reader.GetString(4),
                    AccountType = reader.GetString(5),
                    OwnerName = reader.GetString(6)
                });
            }
            return list;
        }
        public List<Account> GetAccountsFull()
        {
            var list = new List<Account>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand(@"
                SELECT a.AccountID, a.ClientID, a.AccountNumber, a.Balance, 
                       a.Currency, a.Status, a.AccountType, a.OpenDate,
                       c.FullName
                FROM Accounts a
                JOIN Clients c ON c.ClientID = a.ClientID
                ORDER BY a.OpenDate DESC", connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Account
                {
                    AccountID = reader.GetInt32(0),
                    ClientID = reader.GetInt32(1),
                    AccountNumber = reader.GetString(2),
                    Balance = reader.GetDecimal(3),
                    Currency = reader.GetString(4).Trim(),
                    Status = reader.GetString(5),
                    AccountType = reader.GetString(6),
                    OpenDate = reader.GetDateTime(7),
                    OwnerName = reader.GetString(8)
                });
            }
            return list;
        }
        public List<RiskLabel> GetRiskLabels()
        {
            var list = new List<RiskLabel>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand(
                "SELECT LabelID, LabelName, Description, Severity FROM RiskLabels ORDER BY LabelID", connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new RiskLabel
                {
                    LabelID = reader.GetInt32(0),
                    LabelName = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Severity = reader.GetString(3)
                });
            }
            return list;
        }

        public int CreateRiskLabel(string name, string description, string severity)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand(
                @"INSERT INTO RiskLabels (LabelName, Description, Severity)
                  VALUES (@name, @desc, @sev);
                  SELECT SCOPE_IDENTITY();", connection);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@desc", description);
            cmd.Parameters.AddWithValue("@sev", severity);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void UpdateRiskLabel(int id, string name, string description, string severity)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand(
                @"UPDATE RiskLabels SET LabelName=@name, Description=@desc, Severity=@sev
                  WHERE LabelID=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@desc", description);
            cmd.Parameters.AddWithValue("@sev", severity);
            cmd.ExecuteNonQuery();
        }

        public void DeleteRiskLabel(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand("DELETE FROM RiskLabels WHERE LabelID=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
        public Dictionary<string, int> GetRiskDistribution()
        {
            var dict = new Dictionary<string, int>
            {
                { "Low", 0 }, { "Medium", 0 }, { "High", 0 }, { "Critical", 0 }
            };
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand(
                "SELECT RiskLevel, COUNT(*) FROM RiskScores GROUP BY RiskLevel", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var level = reader.GetString(0);
                if (dict.ContainsKey(level))
                    dict[level] = reader.GetInt32(1);
            }
            return dict;
        }

        public List<(string Type, int Count, decimal Sum)> GetTransactionsByType()
        {
            var list = new List<(string, int, decimal)>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand(
                @"SELECT TransactionType, COUNT(*), SUM(Amount)
                  FROM Transactions GROUP BY TransactionType
                  ORDER BY COUNT(*) DESC", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add((reader.GetString(0), reader.GetInt32(1), reader.GetDecimal(2)));
            }
            return list;
        }

        public List<(string Name, int TxCount, double Score)> GetTopClients(int top = 5)
        {
            var list = new List<(string, int, double)>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand($@"
                SELECT TOP {top} c.FullName, COUNT(t.TransactionID), c.ScoringScore
                FROM Clients c
                JOIN Accounts a ON a.ClientID = c.ClientID
                JOIN Transactions t ON t.SenderAccountID = a.AccountID
                GROUP BY c.FullName, c.ScoringScore
                ORDER BY COUNT(t.TransactionID) DESC", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add((reader.GetString(0), reader.GetInt32(1), reader.GetDouble(2)));
            }
            return list;
        }
        public int CreateClient(string fullName, string phone, string email, string passportSeries, string passportNumber, DateTime dateOfBirth)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand(@"
                INSERT INTO Clients (FullName, Phone, Email, PassportSeries, PassportNumber, DateOfBirth)
                VALUES (@name, @phone, @email, @series, @number, @dob);
                SELECT SCOPE_IDENTITY();", connection);
            cmd.Parameters.AddWithValue("@name", fullName);
            cmd.Parameters.AddWithValue("@phone", phone);
            cmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? DBNull.Value : email);
            cmd.Parameters.AddWithValue("@series", passportSeries);
            cmd.Parameters.AddWithValue("@number", passportNumber);
            cmd.Parameters.AddWithValue("@dob", dateOfBirth);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        public int CreateAccount(int clientId, string accountNumber, decimal balance, string accountType)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand(@"
                INSERT INTO Accounts (ClientID, AccountNumber, Balance, Currency, OpenDate, Status, AccountType)
                VALUES (@clientId, @number, @balance, N'RUB', GETDATE(), N'Active', @type);
                SELECT SCOPE_IDENTITY();", connection);
            cmd.Parameters.AddWithValue("@clientId", clientId);
            cmd.Parameters.AddWithValue("@number", accountNumber);
            cmd.Parameters.AddWithValue("@balance", balance);
            cmd.Parameters.AddWithValue("@type", accountType);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        public int CreateCounterparty(string name, string taxId, string category, string riskLevel, bool isBlacklisted, string country)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand(@"
                INSERT INTO Counterparties (Name, TaxID, ActivityCategory, RiskLevel, IsBlacklisted, CountryOfRegistry)
                VALUES (@name, @tax, @cat, @risk, @bl, @country);
                SELECT SCOPE_IDENTITY();", connection);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@tax", taxId);
            cmd.Parameters.AddWithValue("@cat", string.IsNullOrEmpty(category) ? DBNull.Value : category);
            cmd.Parameters.AddWithValue("@risk", riskLevel);
            cmd.Parameters.AddWithValue("@bl", isBlacklisted);
            cmd.Parameters.AddWithValue("@country", country);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
