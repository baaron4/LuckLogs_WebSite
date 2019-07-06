namespace LuckLogsApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Custom : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RaidLogs", "killDate", c => c.String());
            AddColumn("dbo.RaidLogs", "uploaderID", c => c.Int(nullable: false));
            AddColumn("dbo.RaidLogs", "relRate", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.RaidLogs", "relRate");
            DropColumn("dbo.RaidLogs", "uploaderID");
            DropColumn("dbo.RaidLogs", "killDate");
        }
    }
}
