using System.Data;
using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public class ExpenseService : IExpenseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpenseService> _logger;
    
    public string? LastError { get; private set; }
    public bool UsingDummyData { get; private set; }

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection connection string not found");
    }

    private async Task<SqlConnection> CreateConnectionAsync()
    {
        var connectionString = GetConnectionString();
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private void SetError(Exception ex, string context)
    {
        LastError = $"Error in {context}: {ex.Message}";
        
        if (ex.Message.Contains("Managed Identity") || ex.Message.Contains("Azure AD") || ex.Message.Contains("Authentication"))
        {
            LastError += "\n\nManaged Identity Issue: Ensure the managed identity has been assigned to the App Service " +
                        "and has been granted db_datareader, db_datawriter, and EXECUTE permissions on the database. " +
                        "Also ensure AZURE_CLIENT_ID environment variable is set to the managed identity's client ID.";
        }
        
        _logger.LogError(ex, "Error in {Context}", context);
        UsingDummyData = true;
    }

    #region Expense Operations

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetAllExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var expenses = new List<Expense>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            UsingDummyData = false;
            LastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetAllExpensesAsync");
            return GetDummyExpenses();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetExpenseById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                UsingDummyData = false;
                LastError = null;
                return MapExpense(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetExpenseByIdAsync");
            return GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);
        }
    }

    public async Task<List<Expense>> GetExpensesByStatusAsync(string statusName)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetExpensesByStatus", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@StatusName", statusName);

            var expenses = new List<Expense>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            UsingDummyData = false;
            LastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetExpensesByStatusAsync");
            return GetDummyExpenses().Where(e => e.StatusName == statusName).ToList();
        }
    }

    public async Task<List<Expense>> GetExpensesByUserAsync(int userId)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetExpensesByUser", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", userId);

            var expenses = new List<Expense>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            UsingDummyData = false;
            LastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetExpensesByUserAsync");
            return GetDummyExpenses().Where(e => e.UserId == userId).ToList();
        }
    }

    public async Task<List<Expense>> GetPendingExpensesAsync()
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetPendingExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var expenses = new List<Expense>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            UsingDummyData = false;
            LastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetPendingExpensesAsync");
            return GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList();
        }
    }

    public async Task<List<Expense>> SearchExpensesAsync(string? searchTerm, int? categoryId, int? statusId, int? userId, DateTime? startDate, DateTime? endDate)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_SearchExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@SearchTerm", (object?)searchTerm ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", (object?)categoryId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StatusId", (object?)statusId ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);

            var expenses = new List<Expense>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            UsingDummyData = false;
            LastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            SetError(ex, "SearchExpensesAsync");
            var results = GetDummyExpenses();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                results = results.Where(e => 
                    (e.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    e.CategoryName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return results;
        }
    }

    public async Task<int> CreateExpenseAsync(ExpenseCreateRequest request)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_CreateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            
            var outputParam = new SqlParameter("@ExpenseId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(outputParam);

            await command.ExecuteNonQueryAsync();
            
            UsingDummyData = false;
            LastError = null;
            return (int)outputParam.Value;
        }
        catch (Exception ex)
        {
            SetError(ex, "CreateExpenseAsync");
            throw;
        }
    }

    public async Task UpdateExpenseAsync(int expenseId, ExpenseUpdateRequest request)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_UpdateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
            
            UsingDummyData = false;
            LastError = null;
        }
        catch (Exception ex)
        {
            SetError(ex, "UpdateExpenseAsync");
            throw;
        }
    }

    public async Task DeleteExpenseAsync(int expenseId)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_DeleteExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await command.ExecuteNonQueryAsync();
            
            UsingDummyData = false;
            LastError = null;
        }
        catch (Exception ex)
        {
            SetError(ex, "DeleteExpenseAsync");
            throw;
        }
    }

    public async Task SubmitExpenseAsync(int expenseId)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_SubmitExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await command.ExecuteNonQueryAsync();
            
            UsingDummyData = false;
            LastError = null;
        }
        catch (Exception ex)
        {
            SetError(ex, "SubmitExpenseAsync");
            throw;
        }
    }

    public async Task ApproveExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_ApproveExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewedBy", reviewedBy);

            await command.ExecuteNonQueryAsync();
            
            UsingDummyData = false;
            LastError = null;
        }
        catch (Exception ex)
        {
            SetError(ex, "ApproveExpenseAsync");
            throw;
        }
    }

    public async Task RejectExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_RejectExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewedBy", reviewedBy);

            await command.ExecuteNonQueryAsync();
            
            UsingDummyData = false;
            LastError = null;
        }
        catch (Exception ex)
        {
            SetError(ex, "RejectExpenseAsync");
            throw;
        }
    }

    #endregion

    #region Category Operations

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetAllCategories", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var categories = new List<Category>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }

            UsingDummyData = false;
            LastError = null;
            return categories;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetAllCategoriesAsync");
            return GetDummyCategories();
        }
    }

    public async Task<Category?> GetCategoryByIdAsync(int categoryId)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetCategoryById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@CategoryId", categoryId);

            await using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                UsingDummyData = false;
                LastError = null;
                return new Category
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetCategoryByIdAsync");
            return GetDummyCategories().FirstOrDefault(c => c.CategoryId == categoryId);
        }
    }

    #endregion

    #region Status Operations

    public async Task<List<ExpenseStatus>> GetAllStatusesAsync()
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetAllStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var statuses = new List<ExpenseStatus>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                });
            }

            UsingDummyData = false;
            LastError = null;
            return statuses;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetAllStatusesAsync");
            return GetDummyStatuses();
        }
    }

    #endregion

    #region User Operations

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetAllUsers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var users = new List<User>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }

            UsingDummyData = false;
            LastError = null;
            return users;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetAllUsersAsync");
            return GetDummyUsers();
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetUserById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                UsingDummyData = false;
                LastError = null;
                return MapUser(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetUserByIdAsync");
            return GetDummyUsers().FirstOrDefault(u => u.UserId == userId);
        }
    }

    public async Task<List<User>> GetManagersAsync()
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            await using var command = new SqlCommand("sp_GetManagers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var users = new List<User>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                    RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }

            UsingDummyData = false;
            LastError = null;
            return users;
        }
        catch (Exception ex)
        {
            SetError(ex, "GetManagersAsync");
            return GetDummyUsers().Where(u => u.RoleName == "Manager").ToList();
        }
    }

    #endregion

    #region Mapping Helpers

    private static Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            UserEmail = reader.GetString(reader.GetOrdinal("UserEmail")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            AmountGBP = reader.GetDecimal(reader.GetOrdinal("AmountGBP")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewerName = reader.IsDBNull(reader.GetOrdinal("ReviewerName")) ? null : reader.GetString(reader.GetOrdinal("ReviewerName")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static User MapUser(SqlDataReader reader)
    {
        return new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
            ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    #endregion

    #region Dummy Data

    private static List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new()
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Alice Example",
                UserEmail = "alice@example.co.uk",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 12000,
                AmountGBP = 120.00m,
                Currency = "GBP",
                ExpenseDate = DateTime.Today.AddDays(-10),
                Description = "Taxi from airport to client site",
                SubmittedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            },
            new()
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Alice Example",
                UserEmail = "alice@example.co.uk",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 6900,
                AmountGBP = 69.00m,
                Currency = "GBP",
                ExpenseDate = DateTime.Today.AddDays(-20),
                Description = "Client lunch meeting",
                SubmittedAt = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-4)
            },
            new()
            {
                ExpenseId = 3,
                UserId = 1,
                UserName = "Alice Example",
                UserEmail = "alice@example.co.uk",
                CategoryId = 3,
                CategoryName = "Supplies",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 9950,
                AmountGBP = 99.50m,
                Currency = "GBP",
                ExpenseDate = DateTime.Today.AddDays(-30),
                Description = "Office stationery",
                SubmittedAt = DateTime.UtcNow.AddDays(-10),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.UtcNow.AddDays(-8),
                CreatedAt = DateTime.UtcNow.AddDays(-11)
            },
            new()
            {
                ExpenseId = 4,
                UserId = 1,
                UserName = "Alice Example",
                UserEmail = "alice@example.co.uk",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 1920,
                AmountGBP = 19.20m,
                Currency = "GBP",
                ExpenseDate = DateTime.Today.AddDays(-40),
                Description = "Transport to meeting",
                SubmittedAt = DateTime.UtcNow.AddDays(-15),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.UtcNow.AddDays(-14),
                CreatedAt = DateTime.UtcNow.AddDays(-16)
            }
        };
    }

    private static List<Category> GetDummyCategories()
    {
        return new List<Category>
        {
            new() { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new() { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new() { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new() { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new() { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private static List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new() { StatusId = 1, StatusName = "Draft" },
            new() { StatusId = 2, StatusName = "Submitted" },
            new() { StatusId = 3, StatusName = "Approved" },
            new() { StatusId = 4, StatusName = "Rejected" }
        };
    }

    private static List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new()
            {
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                RoleId = 1,
                RoleName = "Employee",
                ManagerId = 2,
                ManagerName = "Bob Manager",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new()
            {
                UserId = 2,
                UserName = "Bob Manager",
                Email = "bob.manager@example.co.uk",
                RoleId = 2,
                RoleName = "Manager",
                ManagerId = null,
                ManagerName = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-12)
            }
        };
    }

    #endregion
}
