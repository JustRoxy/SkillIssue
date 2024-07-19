namespace SkillIssue.Common;

public interface IMergable<T>
{
    static abstract T Merge(T before, T after);
}