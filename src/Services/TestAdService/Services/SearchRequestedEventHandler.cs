﻿using MssDevLab.Common.Models;
using MssDevLab.CommonCore.Interfaces.EventBus;

namespace MssDevLab.TestAdService.Services
{
    public class SearchRequestedEventHandler : IIntegrationEventHandler<SearchRequestedEvent>
    {
        private readonly ILogger<SearchRequestedEventHandler> _logger;
        private readonly ISearchService _searchService;
        private readonly INotificationService _notificationService;

        public SearchRequestedEventHandler(ILogger<SearchRequestedEventHandler> logger,
            INotificationService notificationService,
            ISearchService searchService)
        {
            _logger = logger;
            _searchService = searchService;
            _notificationService = notificationService;
        }

        public async Task Handle(SearchRequestedEvent @event)
        {
            _logger.LogDebug("----- Handling SearchRequestedEvent: eventid={IntegrationEventId}; ConnectionId: {ConnectionId} - ({@IntegrationEvent})",
                @event.Id,
                @event.ConnectionId,
                @event);

            @event.PageSize = 1; // Just for debugging

            var searchRes = await _searchService.FetchAdsAsync(@event);
            _notificationService.PublishSearchResult(searchRes);
        }
    }
}
