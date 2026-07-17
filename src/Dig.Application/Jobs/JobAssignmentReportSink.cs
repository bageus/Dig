using Dig.Domain.Core;

namespace Dig.Application.Jobs
{

public interface IJobAssignmentReportSink
{
    void Record(JobAssignmentReport report);
}

public interface IJobAssignmentReportSource
{
    JobAssignmentReport? Find(EntityId jobId);
}
}
