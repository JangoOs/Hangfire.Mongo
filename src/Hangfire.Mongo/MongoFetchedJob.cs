﻿using System;
using Hangfire.Mongo.Database;
using Hangfire.Mongo.Dto;
using Hangfire.Storage;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Hangfire.Mongo
{
    /// <summary>
    /// Hangfire fetched job for Mongo database
    /// </summary>
    public sealed class MongoFetchedJob : IFetchedJob
    {
        private readonly HangfireDbContext _database;
        private readonly ObjectId _id;

        private bool _disposed;

        private bool _removedFromQueue;

        private bool _requeued;

        /// <summary>
        /// Constructs fetched job by database connection, identifier, job ID and queue
        /// </summary>
        /// <param name="database">Database connection</param>
        /// <param name="id">Identifier</param>
        /// <param name="jobId">Job ID</param>
        /// <param name="queue">Queue name</param>
        public MongoFetchedJob(HangfireDbContext database, ObjectId id, string jobId, string queue)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _id = id;
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            Queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        /// <summary>
        /// Job ID
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Queue name
        /// </summary>
        public string Queue { get; }

        /// <summary>
        /// Removes fetched job from a queue
        /// </summary>
        public void RemoveFromQueue()
        {
            _database
               .JobQueue
               .DeleteOne(Builders<JobQueueDto>.Filter.Eq(_ => _.Id, _id));

            _removedFromQueue = true;
        }

        /// <summary>
        /// Puts fetched job into a queue
        /// </summary>
        public void Requeue()
        {
            _database.JobQueue.FindOneAndUpdate(
                Builders<JobQueueDto>.Filter.Eq(_ => _.Id, _id),
                Builders<JobQueueDto>.Update.Set(_ => _.FetchedAt, null));

            _requeued = true;
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            if (!_removedFromQueue && !_requeued)
            {
                Requeue();
            }

            _disposed = true;
        }
    }
}