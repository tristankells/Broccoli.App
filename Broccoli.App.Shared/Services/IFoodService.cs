﻿using Broccoli.Data.Models;

namespace Broccoli.Shared.Services
{
    public interface IFoodService
    {
        bool TryGetFood(string name, out Food food);
        bool TryGetFoodFuzzy(string name, int maxDistance, out Food food);
        Task<IEnumerable<Food>> GetAllAsync();
    }
}