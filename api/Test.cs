using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tractor.api.author
{
    public static class Test
    {

        // This test shows how a two task job can be created with as few
        //     statements as possible.
        //     
        public static void test_short()
        {
            var jobDesc = new Hashtable()
            {
                { "title", "two layer job" },
                { "priority", 10 },
                { "after", new DateTime(2012, 12, 14, 16, 24, 5) }
            };
            var job = new Job(jobDesc);
            var compTask = job.newTask(new Hashtable() { { "title", "comp" } }, "comp fg.tif bg.tif final.tif");
            var fgTask = compTask.newTask(new Hashtable() { { "title", "render fg" } }, "prman foreground.rib");
            var bgTask = compTask.newTask(new Hashtable() { { "title", "render bg" } }, "prman foreground.rib");
            Console.WriteLine(job);
        }

        // // This test shows how a two task job can be built with many more
        // //     statements.
        // //     
        // public static object test_long()
        // {
        //     var job = new Job();
        //     job.title = "two layer job";
        //     job.priority = 10;
        //     job.after = datetime.datetime(2012, 12, 14, 16, 24, 5);
        //     var fgTask = author.Task();
        //     fgTask.title = "render fg";
        //     var fgCommand = author.Command();
        //     fgCommand.argv = "prman foreground.rib";
        //     fgTask.addCommand(fgCommand);
        //     var bgTask = author.Task();
        //     bgTask.title = "render bg";
        //     var bgCommand = author.Command();
        //     bgCommand.argv = "prman background.rib";
        //     bgTask.addCommand(bgCommand);
        //     var compTask = author.Task();
        //     compTask.title = "render comp";
        //     var compCommand = author.Command();
        //     compCommand.argv = "comp fg.tif bg.tif final.tif";
        //     compCommand.argv = new List<object> {
        //         "comp"
        //     };
        //     compTask.addCommand(compCommand);
        //     compTask.addChild(fgTask);
        //     compTask.addChild(bgTask);
        //     job.addChild(compTask);
        //     Console.WriteLine(job.asTcl());
        // }

        // // This test covers setting all possible attributes of the Job, Task,
        // //     Command, and Iterate objects.
        // //     
        // public static object test_all()
        // {
        //     var job = new Job();
        //     job.title = "all attributes job";
        //     job.after = datetime.datetime(2012, 12, 14, 16, 24, 5);
        //     job.afterjids = new List<object> {
        //         1234,
        //         5678
        //     };
        //     job.paused = true;
        //     job.tier = "express";
        //     job.projects = new List<object> {
        //         "animation"
        //     };
        //     job.atleast = 2;
        //     job.atmost = 4;
        //     job.serialsubtasks = true;
        //     job.spoolcwd = "/some/path/cwd";
        //     job.newDirMap(src: "X:/", dst: "//fileserver/projects", zone: "UNC");
        //     job.newDirMap(src: "X:/", dst: "/fileserver/projects", zone: "NFS");
        //     job.etalevel = 5;
        //     job.tags = new List<object> {
        //         "tag1",
        //         "tag2",
        //         "tag3"
        //     };
        //     job.priority = 10;
        //     job.service = "linux||mac";
        //     job.envkey = new List<object> {
        //         "ej1",
        //         "ej2"
        //     };
        //     job.comment = "this is a great job";
        //     job.metadata = "show=rat shot=food";
        //     job.editpolicy = "canadians";
        //     job.addCleanup(author.Command(argv: "/bin/cleanup this"));
        //     job.newCleanup(argv: new List<object> {
        //         "/bin/cleanup",
        //         "that"
        //     });
        //     job.addPostscript(author.Command(argv: new List<object> {
        //         "/bin/post",
        //         "this"
        //     }));
        //     job.newPostscript(argv: "/bin/post that");
        //     var compTask = author.Task();
        //     compTask.title = "render comp";
        //     compTask.resumeblock = true;
        //     var compCommand = author.Command();
        //     compCommand.argv = "comp /tmp/*";
        //     compTask.addCommand(compCommand);
        //     job.addChild(compTask);
        //     foreach (var i in Enumerable.Range(0, 2))
        //     {
        //         var task = author.Task();
        //         task.title = String.Format("render layer %d", i);
        //         task.id = String.Format("id%d", i);
        //         task.chaser = String.Format("chase file%i", i);
        //         task.preview = String.Format("preview file%i", i);
        //         task.service = "services&&more";
        //         task.atleast = 7;
        //         task.atmost = 8;
        //         task.serialsubtasks = 0;
        //         task.metadata = String.Format("frame=%d", i);
        //         task.addCleanup(author.Command(argv: String.Format("/bin/cleanup file%i", i)));
        //         var command = author.Command(local: @bool(i % 2));
        //         command.argv = String.Format("prman layer%d.rib", i);
        //         command.msg = "command message";
        //         command.service = "cmdservice&&more";
        //         command.tags = new List<object> {
        //             "tagA",
        //             "tagB"
        //         };
        //         command.metrics = "metrics string";
        //         command.id = String.Format("cmdid%i", i);
        //         command.refersto = String.Format("refersto%i", i);
        //         command.expand = 0;
        //         command.atleast = 1;
        //         command.atmost = 5;
        //         command.minrunsecs = 8;
        //         command.maxrunsecs = 88;
        //         command.samehost = 1;
        //         command.envkey = new List<object> {
        //             "e1",
        //             "e2"
        //         };
        //         command.retryrc = new List<object> {
        //             1,
        //             3,
        //             5,
        //             7,
        //             9
        //         };
        //         command.resumewhile = new List<object> {
        //             "/usr/bin/grep",
        //             "-q",
        //             "Checkpoint",
        //             String.Format("file.%d.exr", i)
        //         };
        //         command.resumepin = @bool(i);
        //         command.metadata = String.Format("command metadata %i", i);
        //         task.addCommand(command);
        //         compTask.addChild(task);
        //     }
        //     var iterate = author.Iterate();
        //     iterate.varname = "i";
        //     iterate.frm = 1;
        //     iterate.to = 10;
        //     iterate.addToTemplate(author.Task(title: "process task", argv: "process command"));
        //     iterate.addChild(author.Task(title: "process task", argv: "ls -l"));
        //     job.addChild(iterate);
        //     var instance = author.Instance(title: "id1");
        //     job.addChild(instance);
        //     Console.WriteLine(job.asTcl());
        // }

        // // This test checks that an instance will be created when a task is
        // //     added as a child to more than one task.
        // //     
        // public static object test_instance()
        // {
        //     var job = new Job(title: "two layer job");
        //     var compTask = job.newTask(title: "comp", argv: "comp fg.tif bg.tif final.tif");
        //     var fgTask = compTask.newTask(title: "render fg", argv: "prman foreground.rib");
        //     var bgTask = compTask.newTask(title: "render bg", argv: "prman foreground.rib");
        //     var ribgen = author.Task(title: "ribgen", argv: "ribgen 1-10");
        //     fgTask.addChild(ribgen);
        //     bgTask.addChild(ribgen);
        //     Console.WriteLine(job);
        // }

        // // This test verifies that an interate object cannot be a child to
        // //     more than one task.
        // //     
        // public static object test_double_add()
        // {
        //     var iterate = author.Iterate();
        //     iterate.varname = "i";
        //     iterate.frm = 1;
        //     iterate.to = 10;
        //     iterate.addToTemplate(author.Task(title: "process task", argv: "process command"));
        //     iterate.addChild(author.Task(title: "process task", argv: "ls -l"));
        //     var t1 = author.Task(title: "1");
        //     var t2 = author.Task(title: "2");
        //     t1.addChild(iterate);
        //     try
        //     {
        //         t2.addChild(iterate);
        //     }
        //     catch
        //     {
        //         Console.WriteLine(String.Format("Good, we expected to get an exception for adding a iterate to two parents: %s", err.ToString()));
        //     }
        // }

        // // This test verifies that an exception is raised when trying to set
        // //     an invalid attribute.
        // //     
        // public static object test_bad_attr()
        // {
        //     var job = new Job();
        //     try
        //     {
        //         job.title = "okay to set title";
        //         job.foo = "not okay to set foo";
        //     }
        //     catch (AttributeError)
        //     {
        //         Console.WriteLine(String.Format("Good, we expected to get an exception for setting an invalid attribute: %s", err.ToString()));
        //     }
        // }

        // // This tests the spool method on a job.
        // public static object test_spool()
        // {
        //     var job = new Job(title: "two layer job", priority: 10, after: datetime.datetime(2012, 12, 14, 16, 24, 5));
        //     var compTask = job.newTask(title: "comp", argv: "comp fg.tif bg.tif out.tif", service: "pixarRender");
        //     var fgTask = compTask.newTask(title: "render fg", argv: "prman foreground.rib", service: "pixarRender");
        //     var bgTask = compTask.newTask(title: "render bg", argv: "prman foreground.rib", service: "pixarRender");
        //     //print(job.spool(spoolfile="/spool/file", spoolhost="spoolhost", hostname="myengine", port=8080))
        //     Console.WriteLine(job.spool(spoolfile: "/spool/file", spoolhost: "spoolhost"));
        // }

        // // This builds a job with varios postscript commands.  Submit the
        // //     job to ensure that only the "none", "always", and "done"
        // //     postscript commands run.
        // //     
        // public static object test_postscript()
        // {
        //     var job = new Job(title: "Test Postscript Done");
        //     job.newTask(title: "sleep", argv: "sleep 1", service: "pixarRender");
        //     job.newPostscript(argv: "touch /tmp/postscript.none.%j", service: "pixarRender");
        //     job.newPostscript(argv: "touch /tmp/postscript.done.%j", when: "done", service: "pixarRender");
        //     job.newPostscript(argv: "touch /tmp/postscript.error.%j", when: "error", service: "pixarRender");
        //     job.newPostscript(argv: "touch /tmp/postscript.always.%j", when: "always", service: "pixarRender");
        //     try
        //     {
        //         job.newPostscript(argv: "touch /tmp/postscript.always.%j", when: "nope");
        //     }
        //     catch (TypeError)
        //     {
        //         Console.WriteLine(String.Format("Good, we caught an invalid value for when: %s", err.ToString()));
        //     }
        //     Console.WriteLine(job.asTcl());
        // }

        // // This builds a job with varios postscript commands.  Submit the
        // //     job to ensure that only the "none", "always", and "error"
        // //     postscript commands run.
        // //     
        // public static object test_postscript_error()
        // {
        //     var job = new Job(title: "Test Postscript Error");
        //     job.newTask(title: "fail", argv: "/bin/false", service: "pixarRender");
        //     job.newPostscript(argv: "touch /tmp/postscript.none.%j", service: "pixarRender");
        //     job.newPostscript(argv: "touch /tmp/postscript.done.%j", when: "done", service: "pixarRender");
        //     job.newPostscript(argv: "touch /tmp/postscript.error.%j", when: "error", service: "pixarRender");
        //     job.newPostscript(argv: "touch /tmp/postscript.always.%j", when: "always", service: "pixarRender");
        //     try
        //     {
        //         job.newPostscript(argv: "touch /tmp/postscript.always.%j", when: "nope");
        //     }
        //     catch (TypeError)
        //     {
        //         Console.WriteLine(String.Format("Good, we caught an invalid value for when: %s", err.ToString()));
        //     }
        //     Console.WriteLine(job.asTcl());
        // }

        public static void Run()
        {
            test_short();
            // test_long();
            // test_all();
            // test_instance();
            // test_double_add();
            // test_bad_attr();
            // test_postscript();
            // test_postscript_error();
        }
    }
}