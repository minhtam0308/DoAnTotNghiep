using System.Threading.Tasks;
using SapaFreshWayForStaff.DTOs.CounterStaff;

namespace SapaFreshWayForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Interface for Counter Transaction API Service
    /// </summary>
    public interface ICounterTransactionApiService
    {
        /// <summary>
        /// Lấy danh sách transaction history với filter và phân trang
        /// </summary>
        Task<TransactionHistoryListDto?> GetTransactionHistoryAsync(TransactionFilterDto filter);

        /// <summary>
        /// Export transactions to Excel
        /// </summary>
        Task<byte[]?> ExportTransactionsToExcelAsync(TransactionFilterDto filter);
    }
}

