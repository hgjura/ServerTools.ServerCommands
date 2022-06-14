﻿using FluentAssertions.Specialized;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands
{
    public interface ICloudCommands
    {
        #region Single Commands functionality
        public Task<bool> PostCommandAsync<T>(dynamic CommandContext, CommandMetadata PreviousMatadata = new());

        public Task<bool> PostCommandAsync(Type type, dynamic CommandContext, CommandMetadata PreviousMatadata = new());

        public Task<bool> PostCommandAsync(string type_name, dynamic CommandContext, CommandMetadata PreviousMatadata = new());

        public Task<(bool, int, List<string>)> ExecuteCommandsAsync(int timeWindowinMinutes = 1);

        #endregion

        

        #region Single Response functionality
        public Task<bool> PostResponseAsync<T>(dynamic ResponseContext, CommandMetadata OriginalCommandMetadata);

        public Task<bool> PostResponseAsync(Type ResponseType, dynamic ResponseContext, CommandMetadata OriginalCommandMetadata);

        public Task<(bool, int, List<string>)> ExecuteResponsesAsync(int timeWindowinMinutes = 1);

        #endregion

        public Task ClearAllAsync();
    }
}
