﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VibeChat.Web;

namespace Vibechat.Web.Data.DataModels
{
    public class ConversationsBansDataModel
    {
        public int Id { get; set; }
        public UserInApplication BannedUser { get; set; }

        public ConversationDataModel Conversation { get; set; }
    }
}
