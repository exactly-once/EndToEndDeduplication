﻿using ExactlyOnce.Core;
using ExactlyOnce.NServiceBus;
using ExactlyOnce.NServiceBus.Cosmos;

// Should be visible without having to add reference
// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public static class TransactionContextExtensions
    {
        public static ITransactionBatchContext Batch(this ITransactionContext transactionContext)
        {
            var recordContainer = (TransactionRecordContainer)transactionContext.TransactionRecordContainer.Unwrap();
            return recordContainer.BatchContext;
        }

        public static ITransactionBatchContext TransactionBatch(this IHumanInterfaceConnectorMessageSession session)
        {
            return session.TransactionContext.Batch();
        }

        public static ITransactionBatchContext TransactionBatch<T>(this IMachineInterfaceConnectorMessageSession<T> session)
        {
            return session.TransactionContext.Batch();
        }

        public static ITransactionBatchContext TransactionBatch(this IMessageHandlerContext handlerContext)
        {
            return handlerContext.TransactionContext().Batch();
        }
    }
}