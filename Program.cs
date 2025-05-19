using System;
using System.Data.SqlClient;

namespace BankingAppUserInput
{
    class Program
    {
        static string connectionString = "Server=LTIN594416;Database=BankingDB;Trusted_Connection=True;";

        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("\nBanking System Menu:");
                Console.WriteLine("1. Create Customer");
                Console.WriteLine("2. Create Account");
                Console.WriteLine("3. Deposit");
                Console.WriteLine("4. Withdraw");
                Console.WriteLine("5. Exit");
                Console.Write("Enter your choice: ");

                string choice = Console.ReadLine();

                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        switch (choice)
                        {
                            case "1":
                                CreateNewCustomer(connection);
                                break;
                            case "2":
                                CreateNewAccount(connection);
                                break;
                            case "3":
                                PerformDeposit(connection);
                                break;
                            case "4":
                                PerformWithdrawal(connection);
                                break;
                            case "5":
                                Console.WriteLine("Exiting the banking system. Goodbye!");
                                return;
                            default:
                                Console.WriteLine("Invalid choice. Please try again.");
                                break;
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Database error occurred: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        static void CreateNewCustomer(SqlConnection connection)
        {
            Console.Write("Enter customer name: ");
            string name = Console.ReadLine();
            Console.Write("Enter customer email: ");
            string email = Console.ReadLine();

            string query = "INSERT INTO Customers (Name, Email) VALUES (@Name, @Email)";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Email", email);
                command.ExecuteNonQuery();
                Console.WriteLine($"Customer '{name}' created successfully.");
            }
        }

        static void CreateNewAccount(SqlConnection connection)
        {
            Console.Write("Enter Customer ID for the new account: ");
            if (int.TryParse(Console.ReadLine(), out int customerId))
            {
                // Check if the CustomerID exists in the Customers table
                string checkCustomerQuery = "SELECT COUNT(*) FROM Customers WHERE CustomerID = @CustomerID";
                using (SqlCommand checkCommand = new SqlCommand(checkCustomerQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@CustomerID", customerId);
                    int customerExists = (int)checkCommand.ExecuteScalar();

                    if (customerExists > 0)
                    {
                        Console.Write("Enter initial balance: ");
                        if (decimal.TryParse(Console.ReadLine(), out decimal initialBalance))
                        {
                            string query = "INSERT INTO Accounts (CustomerID, Balance) OUTPUT INSERTED.AccountID VALUES (@CustomerID, @Balance)";
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@CustomerID", customerId);
                                command.Parameters.AddWithValue("@Balance", initialBalance);
                                int accountId = (int)command.ExecuteScalar();
                                Console.WriteLine($"Account created successfully with ID: {accountId} for Customer ID: {customerId}.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid initial balance. Please enter a numeric value.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Customer with ID {customerId} does not exist. Please create the customer first.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Invalid Customer ID. Please enter a numeric value.");
            }
        }

        static void PerformDeposit(SqlConnection connection)
        {
            Console.Write("Enter Account ID to deposit into: ");
            if (int.TryParse(Console.ReadLine(), out int accountId))
            {
                Console.Write("Enter the amount to deposit: ");
                if (decimal.TryParse(Console.ReadLine(), out decimal amount))
                {
                    if (amount > 0)
                    {
                        PerformTransaction(connection, accountId, amount);
                        Console.WriteLine($"Successfully deposited {amount:C} into account {accountId}.");
                    }
                    else
                    {
                        Console.WriteLine("Deposit amount must be positive.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid deposit amount. Please enter a numeric value.");
                }
            }
            else
            {
                Console.WriteLine("Invalid Account ID. Please enter a numeric value.");
            }
        }

        static void PerformWithdrawal(SqlConnection connection)
        {
            Console.Write("Enter Account ID to withdraw from: ");
            if (int.TryParse(Console.ReadLine(), out int accountId))
            {
                Console.Write("Enter the amount to withdraw: ");
                if (decimal.TryParse(Console.ReadLine(), out decimal amount))
                {
                    if (amount > 0)
                    {
                        if (Withdraw(connection, accountId, amount))
                        {
                            Console.WriteLine($"Successfully withdrew {amount:C} from account {accountId}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Withdrawal amount must be positive.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid withdrawal amount. Please enter a numeric value.");
                }
            }
            else
            {
                Console.WriteLine("Invalid Account ID. Please enter a numeric value.");
            }
        }

        static void PerformTransaction(SqlConnection connection, int accountId, decimal amount)
        {
            string query = "INSERT INTO Transactions (AccountID, Amount, TransactionDate) VALUES (@AccountID, @Amount, @TransactionDate)";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@AccountID", accountId);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
                command.ExecuteNonQuery();

                // Update account balance
                string updateQuery = "UPDATE Accounts SET Balance = Balance + @Amount WHERE AccountID = @AccountID";
                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@Amount", amount);
                    updateCommand.Parameters.AddWithValue("@AccountID", accountId);
                    updateCommand.ExecuteNonQuery();
                }
            }
        }

        static bool Withdraw(SqlConnection connection, int accountId, decimal amount)
        {
            string checkBalanceQuery = "SELECT Balance FROM Accounts WHERE AccountID = @AccountID";
            using (SqlCommand checkBalanceCommand = new SqlCommand(checkBalanceQuery, connection))
            {
                checkBalanceCommand.Parameters.AddWithValue("@AccountID", accountId);
                decimal currentBalance = (decimal)checkBalanceCommand.ExecuteScalar();

                if (currentBalance >= amount)
                {
                    PerformTransaction(connection, accountId, -amount);
                    return true;
                }
                else
                {
                    Console.WriteLine($"Insufficient funds in account {accountId} to withdraw {amount:C}. Current balance: {currentBalance:C}");
                    return false;
                }
            }
        }
    }
}
