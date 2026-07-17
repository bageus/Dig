using System;

namespace Dig.Application.Jobs
{

public interface IJobAssignmentReportSink
{
    void Record(JobAssignmentReport report);
}
}
