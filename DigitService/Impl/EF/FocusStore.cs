﻿using CalendarService.Models;
using Digit.Focus;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitService.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TravelService.Models.Directions;

namespace DigitService.Impl.EF
{
    public static class StoredFocusItemExtension
    {
        public static void MapFromFocusItem(this StoredFocusItem storedFocusItem, FocusItem focusItem)
        {

        }

        public static FocusItem MapToFocusItem(this StoredFocusItem storedFocusItem)
        {
            return new FocusItem()
            {
                Id = storedFocusItem.Id,
                IndicateTime = new DateTimeOffset(storedFocusItem.IndicateAt, TimeSpan.Zero),
                CalendarEventId = storedFocusItem.CalendarEventId,
                CalendarEventFeedId = storedFocusItem.CalendarEventFeedId,
                CalendarEventHash = storedFocusItem.CalendarEvent.CalendarEventHash,
                Start = storedFocusItem.ActiveStart,
                End = storedFocusItem.ActiveEnd,
                DirectionsMetadata = null != storedFocusItem.Directions ? new DirectionsMetadata()
                {
                    Error = storedFocusItem.Directions.PlaceNotFound.GetValueOrDefault(false) ? TravelService.Models.DirectionsNotFoundReason.AddressNotFound : (
                    storedFocusItem.Directions.DirectionsNotFound.GetValueOrDefault(false) ? (TravelService.Models.DirectionsNotFoundReason?)TravelService.Models.DirectionsNotFoundReason.RouteNotFound : null),
                    Key = storedFocusItem.Directions.DirectionsKey,
                    TravelStatus = storedFocusItem.Directions.TravelStatus,
                } : null
            };
        }
    }

    public class FocusStore : IFocusStore
    {
        private readonly DigitServiceContext digitServiceContext;
        private readonly IUserRepository userRepository;

        public FocusStore(DigitServiceContext digitServiceContext, IUserRepository userRepository)
        {
            this.digitServiceContext = digitServiceContext;
            this.userRepository = userRepository;
        }


        public async Task<bool> FocusItemNotifiedAsync(string itemId)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.Id == itemId).SingleOrDefaultAsync();
            await digitServiceContext.Entry(item).ReloadAsync();
            return item.UserNotified;
        }

        public async Task<FocusItem> Get(string userId, string id)
        {
            return await digitServiceContext.FocusItems
                .Include(v => v.CalendarEvent)
                .Include(v => v.Directions)
                .Where(v => v.UserId == userId && v.Id == id)
                .Select(v => v.MapToFocusItem()).SingleOrDefaultAsync();
        }

        public async Task<FocusItem[]> GetActiveAsync(string userId)
        {
            return await GetTimeRangeAsync(userId, DateTimeOffset.Now, DateTimeOffset.Now.AddHours(2));
        }

        public async Task<FocusItem[]> GetCalendarItemsAsync(string userId)
        {
            return (await digitServiceContext.FocusItems
                .Include(v => v.CalendarEvent)
                .Include(v => v.Directions)
                .Where(v => v.UserId == userId && null != v.CalendarEventFeedId && null != v.CalendarEventId)
            .ToArrayAsync())
            .Select(v => v.MapToFocusItem()).ToArray();
        }

        public async Task<FocusItem[]> GetTimeRangeAsync(string userId, DateTimeOffset from, DateTimeOffset to)
        {
            return (await digitServiceContext.FocusItems
                .Include(v => v.CalendarEvent)
                .Include(v => v.Directions)
                .Where(v => v.UserId == userId &&
                    v.ActiveStart < to.UtcDateTime && from.UtcDateTime < v.ActiveEnd)
                .ToArrayAsync())
                .Select(v => v.MapToFocusItem()).ToArray();
        }

        public async Task<string[]> GetUsersWithDirections(string directionsKey)
        {
            return await digitServiceContext.FocusItems.Where(v => null != v.Directions && v.Directions.DirectionsKey == directionsKey)
                .Select(v => v.UserId).Distinct().ToArrayAsync();
        }

        public async Task RemoveAsync(FocusItem evt)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.Id == evt.Id).SingleOrDefaultAsync();
            digitServiceContext.FocusItems.Remove(item);
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task SetFocusItemNotifiedAsync(string itemId)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.Id == itemId).SingleOrDefaultAsync();
            item.UserNotified = true;
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task SetPlaceForItem(string userId, string itemId, Place place)
        {
            var item = await digitServiceContext.FocusItems
                .Include(f => f.Directions)
                .Where(v => v.Id == itemId).SingleAsync();
            if (null == item.Directions)
            {
                item.Directions = new StoredDirectionsInfo();
            }
            item.Directions.Lat = place.Lat;
            item.Directions.Lng = place.Lng;
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task SetTravelStatus(string userId, string itemId, TravelStatus travelStatus)
        {
            var item = await digitServiceContext.FocusItems
              .Include(f => f.Directions)
              .Where(v => v.Id == itemId).SingleAsync();
            if (null != item.Directions)
            {
                item.Directions.TravelStatus = travelStatus;
            }
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task<FocusItem> StoreCalendarEventAsync(string userId, Event evt)
        {
            var user = await userRepository.GetOrCreateAsync(userId);
            var focusItem = new StoredFocusItem()
            {
                Id = Guid.NewGuid().ToString(),
                CalendarEvent = new StoredCalendarEvent()
                {
                    FeedId = evt.FeedId,
                    Id = evt.Id,
                    CalendarEventHash = evt.GenerateHash()
                },
                User = user,
                UserNotified = false,
                ActiveEnd = evt.End.UtcDateTime,
                ActiveStart = (evt.Start - FocusConstants.CalendarServiceInacurracy).UtcDateTime,
                IndicateAt = evt.Start.UtcDateTime,
                Directions = null,
            };
            await digitServiceContext.FocusItems.AddAsync(focusItem);
            await digitServiceContext.SaveChangesAsync();
            return focusItem.MapToFocusItem();
        }

        public async Task<bool> UpdateActiveItem(string userId, string itemId)
        {
            var user = await digitServiceContext.Users
                .Where(v => v.Id == userId).SingleOrDefaultAsync();
            bool matches = false;
            if (null == user)
            {
                return false;
            }
            else
            {
                matches = itemId == user.ActiveFocusItem;
                user.ActiveFocusItem = itemId;
            }
            await digitServiceContext.SaveChangesAsync();
            return !matches;
        }

        public async Task<FocusItem> UpdateCalendarEventAsync(string userId, Event evt)
        {
            var item = await digitServiceContext.FocusItems
                .Include(v => v.CalendarEvent)
                .Include(v => v.Directions)
                .Where(v => v.UserId == userId
                        && v.CalendarEventFeedId == evt.FeedId
                        && v.CalendarEventId == evt.Id)
                        .SingleOrDefaultAsync();
            item.UserNotified = false;
            item.ActiveEnd = evt.End.UtcDateTime;
            item.ActiveStart = (evt.Start - FocusConstants.CalendarServiceInacurracy).UtcDateTime;
            item.CalendarEvent.CalendarEventHash = evt.GenerateHash();
            item.Directions = null;
            await digitServiceContext.SaveChangesAsync();
            return item.MapToFocusItem();
        }

        public async Task UpdateDirections(string itemId, DirectionsResult directionsResult, int? preferredRoute)
        {
            var item = await digitServiceContext.FocusItems
                .Include(v => v.Directions)
                .Where(v => v.Id == itemId).SingleOrDefaultAsync();
            item.Directions = item.Directions ?? new StoredDirectionsInfo();
            item.Directions.DirectionsKey = directionsResult.CacheKey;
            item.Directions.PreferredRoute = null != directionsResult.TransitDirections ? preferredRoute : null;
            item.Directions.PlaceNotFound = null != directionsResult.NotFound && directionsResult.NotFound.Reason == TravelService.Models.DirectionsNotFoundReason.AddressNotFound;
            item.Directions.DirectionsNotFound = null != directionsResult.NotFound && directionsResult.NotFound.Reason == TravelService.Models.DirectionsNotFoundReason.RouteNotFound;
            item.Directions.TravelStatus = TravelStatus.UnStarted;
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task UpdateIndicateTime(string itemId, DateTimeOffset indicateTime)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.Id == itemId).SingleOrDefaultAsync();
            item.IndicateAt = indicateTime.UtcDateTime;
            await digitServiceContext.SaveChangesAsync();
        }
    }
}
