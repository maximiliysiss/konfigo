namespace Konfigo.IntegrationTests.DbHelpers.Shared;

internal interface ITracker<T>
{
    T Track(T entity);
}
