namespace namasdev.Data.EntityCore.Tests.Helpers
{
    public class TestTag
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public string Value { get; set; }

        public TestEntity Entity { get; set; }
    }
}
