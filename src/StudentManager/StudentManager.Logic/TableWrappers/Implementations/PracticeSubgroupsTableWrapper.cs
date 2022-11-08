﻿using LazyCache;
using Serilog;
using StudentManager.Tables;
using StudentManager.Tables.Models;

namespace StudentManager.Logic.TableWrappers.Implementations;

public class PracticeSubgroupsTableWrapper : BaseTableWrapper<SubgroupOfPracticeData>
{
    public PracticeSubgroupsTableWrapper(IGoogleSheet<SubgroupOfPracticeData> sheet, IAppCache appCache, ILogger logger)
        : base(sheet, appCache, logger)
    {
    }
}