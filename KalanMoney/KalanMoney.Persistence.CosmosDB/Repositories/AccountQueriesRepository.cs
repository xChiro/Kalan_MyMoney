using KalanMoney.Domain.Entities;
using KalanMoney.Domain.UseCases.Repositories;
using KalanMoney.Domain.UseCases.Repositories.Models;
using KalanMoney.Persistence.CosmosDB.DTOs;
using Microsoft.Azure.Cosmos;

namespace KalanMoney.Persistence.CosmosDB.Repositories;

public class AccountQueriesRepository : IAccountQueriesRepository
{
    private readonly Container _container;
    private readonly TaskFactory _taskFactory;

    public AccountQueriesRepository(Container container)
    {
        _container = container;
        _taskFactory = new TaskFactory(CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
    }

    public FinancialAccount? GetAccount(string id, TransactionFilter transactionFilter)
    {
        const string sqlQuery = $"SELECT * FROM c WHERE c.id = @idParam OR " +
                                $"TimestampToDateTime(c.transactions.timeStamp) >= @from " +
                                $"AND TimestampToDateTime(c.transactions.timeStamp) <= @to";

        var queryDefinition = new QueryDefinition(sqlQuery)
            .WithParameter("@idParam", id)
            .WithParameter("@from", transactionFilter.From.ToDateTime(TimeOnly.MinValue))
            .WithParameter("@to", transactionFilter.To.ToDateTime(TimeOnly.MaxValue));

        var queryRequestOptions = new QueryRequestOptions()
        {
            MaxItemCount = 1
        };

        using var feedIterator =
            _container.GetItemQueryIterator<FinancialAccountDto>(queryDefinition, null, queryRequestOptions);

        if (!feedIterator.HasMoreResults) throw new KeyNotFoundException("Account.Id not found.");

        var result = _taskFactory
            .StartNew(() => feedIterator.ReadNextAsync())
            .Unwrap()
            .GetAwaiter()
            .GetResult()
            .FirstOrDefault();

        return result?.ToFinancialAccount();
    }

    public FinancialAccount? GetAccountByOwner(string ownerId, TransactionFilter transactionFilter)
    {
        const string sqlQuery = $"SELECT * FROM c WHERE c.owner.subId = @ownerId OR " +
                                $"TimestampToDateTime(c.transactions.timeStamp) >= @from " +
                                $"AND TimestampToDateTime(c.transactions.timeStamp) <= @to";

        var queryDefinition = new QueryDefinition(sqlQuery)
            .WithParameter("@ownerId", ownerId)
            .WithParameter("@from", transactionFilter.From.ToDateTime(TimeOnly.MinValue))
            .WithParameter("@to", transactionFilter.To.ToDateTime(TimeOnly.MaxValue));

        var queryRequestOptions = new QueryRequestOptions()
        {
            MaxItemCount = 1
        };

        using var feedIterator =
            _container.GetItemQueryIterator<FinancialAccountDto>(queryDefinition, null, queryRequestOptions);

        if (!feedIterator.HasMoreResults) throw new KeyNotFoundException("Account with Owner.Id not found.");

        var result = _taskFactory
            .StartNew(() => feedIterator.ReadNextAsync())
            .Unwrap()
            .GetAwaiter()
            .GetResult()
            .FirstOrDefault();

        return result?.ToFinancialAccount();
    }

    public Transaction[] GetMonthlyTransactions(string accountId, int invalidMonth, int year)
    {
        throw new NotImplementedException();
    }
}