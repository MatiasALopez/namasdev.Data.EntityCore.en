using System.Collections.Generic;

namespace namasdev.Data.EntityCore.Tests.Helpers
{
    public class TestCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<TestEntity> Entities { get; set; }
    }
}
