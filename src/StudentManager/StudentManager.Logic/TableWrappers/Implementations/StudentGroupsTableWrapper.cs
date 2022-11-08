﻿using LazyCache;
using Serilog;
using StudentManager.Tables;
using StudentManager.Tables.Models;

namespace StudentManager.Logic.TableWrappers.Implementations;

public class StudentGroupsTableWrapper : BaseTableWrapper<GroupData>
{
    public StudentGroupsTableWrapper(IGoogleSheet<GroupData> sheet, IAppCache appCache, ILogger logger)
        : base(sheet, appCache, logger)
    {
    }
}