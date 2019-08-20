﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vibechat.Web.Data_Layer.DataModels;

namespace Vibechat.Web.Data_Layer.Repositories
{
    public interface IChatEventsRepository : IAsyncRepository<ChatEventDataModel>
    {
    }
}
