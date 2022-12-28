namespace ExactlyOnce.AcceptanceTests.Infrastructure;

public class ChaosMonkeyCollection
{
    public ChaosMonkeyCollection(int post = 0, int put = 0, int delete = 0)
    {
        Post = new ChaosMonkey(post);
        Put = new ChaosMonkey(put);
        Delete = new ChaosMonkey(delete);
    }

    public ChaosMonkey Post { get; }
    public ChaosMonkey Put { get; }
    public ChaosMonkey Delete { get; }

    public override string ToString()
    {
        return $"Post: {Post}, Put: {Put}, Delete: {Delete}";
    }
}