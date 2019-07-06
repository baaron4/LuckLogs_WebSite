namespace LuckLogsApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Custom2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RaidLogs", "groupID", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.RaidLogs", "groupID");
        }
    }
}
