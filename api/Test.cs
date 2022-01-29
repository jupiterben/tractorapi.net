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
            var job = new Job()
            {
                title = "two layer job",
                priority = 10,
                after = new DateTime(2012, 12, 14, 16, 24, 5),
            };
            var compTask = job.newTask(title: "comp", argv: "comp fg.tif bg.tif final.tif");
            var fgTask = compTask.newTask(title: "render fg", argv: "prman foreground.rib");
            var bgTask = compTask.newTask(title: "render bg", argv: "prman foreground.rib");
            Console.WriteLine(job);
        }

        // // This test shows how a two task job can be built with many more
        // //     statements.
        // //     
        public static void test_long()
        {
            var job = new Job();
            job.title = "two layer job";
            job.priority = 10;
            job.after = new DateTime(2012, 12, 14, 16, 24, 5);
            var fgTask = new Task();
            fgTask.title = "render fg";
            var fgCommand = new Command();
            fgCommand.argv = "prman foreground.rib";
            fgTask.addCommand(fgCommand);
            var bgTask = new Task();
            bgTask.title = "render bg";
            var bgCommand = new Command();
            bgCommand.argv = "prman background.rib";
            bgTask.addCommand(bgCommand);
            var compTask = new Task();
            compTask.title = "render comp";
            var compCommand = new Command();
            compCommand.argv = "comp fg.tif bg.tif final.tif";
            compCommand.argv = new List<string> { "comp" };
            compTask.addCommand(compCommand);
            compTask.addChild(fgTask);
            compTask.addChild(bgTask);
            job.addChild(compTask);
            Console.WriteLine(job.asTcl());
        }

        // // This test covers setting all possible attributes of the Job, Task,
        // //     Command, and Iterate objects.
        // //     
        public static void test_all()
        {
            var job = new Job();
            job.title = "all attributes job";
            job.after = new DateTime(2012, 12, 14, 16, 24, 5);
            job.afterjids = new List<int> {
                1234,
                5678
            };
            job.paused = true;
            job.tier = "express";
            job.projects = new List<string> { "animation" };
            job.atleast = 2;
            job.atmost = 4;
            job.serialsubtasks = true;
            job.spoolcwd = "/some/path/cwd";
            job.newDirMap(src: "X:/", dst: "//fileserver/projects", zone: "UNC");
            job.newDirMap(src: "X:/", dst: "/fileserver/projects", zone: "NFS");
            job.etalevel = 5;
            job.tags = new List<string> {
                "tag1",
                "tag2",
                "tag3"
            };
            job.priority = 10;
            job.service = "linux||mac";
            job.envkey = new List<string> {
                "ej1",
                "ej2"
            };
            job.comment = "this is a great job";
            job.metadata = "show=rat shot=food";
            job.editpolicy = "canadians";
            job.addCleanup(new Command(argv: "/bin/cleanup this"));
            job.newCleanup(argv: new List<string> {
                "/bin/cleanup",
                "that"
            });
            job.addPostscript(new Command(argv: new List<string> {
                "/bin/post",
                "this"
            }));
            job.newPostscript(argv: "/bin/post that");
            var compTask = new Task();
            compTask.title = "render comp";
            compTask.resumeblock = true;
            var compCommand = new Command();
            compCommand.argv = "comp /tmp/*";
            compTask.addCommand(compCommand);
            job.addChild(compTask);
            foreach (var i in Enumerable.Range(0, 2))
            {
                var task = new Task();
                task.title = String.Format("render layer {0}", i);
                task.id = String.Format("id{0}", i);
                task.chaser = String.Format("chase file{0}", i);
                task.preview = String.Format("preview file{0}", i);
                task.service = "services&&more";
                task.atleast = 7;
                task.atmost = 8;
                task.serialsubtasks = false;
                task.metadata = String.Format("frame={0}", i);
                task.addCleanup(new Command(argv: String.Format("/bin/cleanup file{0}", i)));
                var command = new Command(local: (i % 2) == 1);
                command.argv = String.Format("prman layer{0}.rib", i);
                command.msg = "command message";
                command.service = "cmdservice&&more";
                command.tags = new List<string> {
                    "tagA",
                    "tagB"
                };
                command.metrics = "metrics string";
                command.id = String.Format("cmdid{0}", i);
                command.refersto = String.Format("refersto{0}", i);
                command.expand = false;
                command.atleast = 1;
                command.atmost = 5;
                command.minrunsecs = 8;
                command.maxrunsecs = 88;
                command.samehost = true;
                command.envkey = new List<string> {
                    "e1",
                    "e2"
                };
                command.retryrc = new List<int> {
                    1,
                    3,
                    5,
                    7,
                    9
                };
                command.resumewhile = new List<string> {
                    "/usr/bin/grep",
                    "-q",
                    "Checkpoint",
                    String.Format("file.{0}.exr", i)
                };
                command.resumepin = (i == 1);
                command.metadata = String.Format("command metadata {0}", i);
                task.addCommand(command);
                compTask.addChild(task);
            }
            var iterate = new Iterate();
            iterate.varname = "i";
            iterate.frm = 1;
            iterate.to = 10;
            iterate.addToTemplate(new Task(title : "process task",argv: "process command"));
            iterate.addChild(new Task(title : "process task",argv: "ls -l") );
            job.addChild(iterate);
            var instance = new Instance(title: "id1");
            job.addChild(instance);
            Console.WriteLine(job.asTcl());
        }

        // // This test checks that an instance will be created when a task is
        // //     added as a child to more than one task.
        // //     
        public static void test_instance()
        {
            var job = new Job(title: "two layer job");
            var compTask = job.newTask(title: "comp", argv: "comp fg.tif bg.tif final.tif");
            var fgTask = compTask.newTask(title: "render fg", argv: "prman foreground.rib");
            var bgTask = compTask.newTask(title: "render bg", argv: "prman foreground.rib");
            var ribgen = new Task(title: "ribgen", argv: "ribgen 1-10");
            fgTask.addChild(ribgen);
            bgTask.addChild(ribgen);
            Console.WriteLine(job);
        }

        // // This test verifies that an interate object cannot be a child to
        // //     more than one task.
        // //     
        public static void test_double_add()
        {
            var iterate = new Iterate();
            iterate.varname = "i";
            iterate.frm = 1;
            iterate.to = 10;
            iterate.addToTemplate(new Task(title: "process task", argv: "process command"));
            iterate.addChild(new Task(title: "process task", argv: "ls -l"));
            var t1 = new Task(title: "1");
            var t2 = new Task(title: "2");
            t1.addChild(iterate);
            try
            {
                t2.addChild(iterate);
            }
            catch(Exception err)
            {
                Console.WriteLine(String.Format("Good, we expected to get an exception for adding a iterate to two parents: {0}", err.Message));
            }
        }

        // // This test verifies that an exception is raised when trying to set
        // //     an invalid attribute.
        // //     
        public static void test_bad_attr()
        {
            var job = new Job();
            try
            {
                job.title = "okay to set title";
                //job.foo = "not okay to set foo";
            }
            catch (AttributeError err)
            {
                Console.WriteLine(String.Format("Good, we expected to get an exception for setting an invalid attribute: {0}", err.Message));
            }
        }

        // // This tests the spool method on a job.
        public static void test_spool()
        {
            var job = new Job(title: "two layer job") { priority = 10, after = new DateTime(2012, 12, 14, 16, 24, 5) };
            var compTask = job.newTask(title: "comp", argv: "comp fg.tif bg.tif out.tif", service: "pixarRender");
            var fgTask = compTask.newTask(title: "render fg", argv: "prman foreground.rib", service: "pixarRender");
            var bgTask = compTask.newTask(title: "render bg", argv: "prman foreground.rib", service: "pixarRender");
            //print(job.spool(spoolfile="/spool/file", spoolhost="spoolhost", hostname="myengine", port=8080))
            //Console.WriteLine(job.spool(spoolfile: "/spool/file", spoolhost: "spoolhost"));
        }

        // // This builds a job with varios postscript commands.  Submit the
        // //     job to ensure that only the "none", "always", and "done"
        // //     postscript commands run.
        // //     
        public static void test_postscript()
        {
            var job = new Job(title: "Test Postscript Done");
            job.newTask(title: "sleep", argv: "sleep 1", service: "pixarRender");
            job.newPostscript(argv: "touch /tmp/postscript.none.%j", service: "pixarRender");
            job.newPostscript(argv: "touch /tmp/postscript.done.%j", when: "done", service: "pixarRender");
            job.newPostscript(argv: "touch /tmp/postscript.error.%j", when: "error", service: "pixarRender");
            job.newPostscript(argv: "touch /tmp/postscript.always.%j", when: "always", service: "pixarRender");
            try
            {
                job.newPostscript(argv: "touch /tmp/postscript.always.%j", when: "nope");
            }
            catch (TypeError err)
            {
                Console.WriteLine(String.Format("Good, we caught an invalid value for when: {0}", err.Message));
            }
            Console.WriteLine(job.asTcl());
        }

        // // This builds a job with varios postscript commands.  Submit the
        // //     job to ensure that only the "none", "always", and "error"
        // //     postscript commands run.
        // //     
        public static void test_postscript_error()
        {
            var job = new Job(title: "Test Postscript Error");
            job.newTask(title: "fail", argv: "/bin/false", service: "pixarRender");
            job.newPostscript(argv: "touch /tmp/postscript.none.%j", service: "pixarRender");
            job.newPostscript(argv: "touch /tmp/postscript.done.%j", when: "done", service: "pixarRender");
            job.newPostscript(argv: "touch /tmp/postscript.error.%j", when: "error", service: "pixarRender");
            job.newPostscript(argv: "touch /tmp/postscript.always.%j", when: "always", service: "pixarRender");
            try
            {
                job.newPostscript(argv: "touch /tmp/postscript.always.%j", when: "nope");
            }
            catch (TypeError err)
            {
                Console.WriteLine(String.Format("Good, we caught an invalid value for when: {0}", err.Message));
            }
            Console.WriteLine(job.asTcl());
        }

        public static void Run()
        {
            test_short();
            test_long();
            test_all();
            test_instance();
            test_double_add();
            test_bad_attr();
            test_postscript();
            test_postscript_error();
        }
    }
}