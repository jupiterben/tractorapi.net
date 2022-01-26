using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace tractor.api.author
{
    // Base class for author module exceptions.
    public class AuthorError
        : Exception
    {
        public AuthorError(string msg) : base(msg) { }
    }

    // Raised if an attribute must be defined when emitting an element.
    public class RequiredValueError
        : AuthorError
    {
        public RequiredValueError(string msg) : base(msg) { }
    }

    // Raised when attempting to add a task to multiple parent tasks.
    public class ParentExistsError
        : AuthorError
    {
        public ParentExistsError(string msg) : base(msg) { }
    }

    // Raised when there is a problem spooling a job.
    public class SpoolError : AuthorError
    {
        public SpoolError(string msg) : base(msg) { }
    }

    public class TypeError : Exception
    {
        public TypeError(string msg) : base(msg) { }
    }

    public class AttributeError : Exception
    {
        public AttributeError(string msg) : base(msg) { }
    }


    public static class Indent
    {
        public static int TclIndentLevel = 0;
        public static int SPACES_PER_INDENT = 2;
        // Return a quantity of spaces for the given indentation level.
        public static string tclIndentStr()
        {
            return new string(' ', TclIndentLevel * SPACES_PER_INDENT);
        }

        // Convert a string to a list of strings.
        public static string[] str2argv(string s)
        {
            return s.Split();
        }
    }

    // The Attribute class presents a way to define the nature of
    //     attributes of job Elements, such as whether or not they are
    //     required and how valid values are determined.
    //     
    public class Attribute
    {
        public string name;
        public string alias;
        public bool required;
        public object value;
        public bool suppressTclKey;

        public Attribute(string name, string alias = null, bool required = false, bool suppressTclKey = false)
        {
            this.name = name;
            this.alias = alias;
            this.required = required;
            this.value = null;
            this.suppressTclKey = suppressTclKey;
        }

        // Return True if the attribute has been set; otherwise, False.
        public virtual bool hasValue()
        {
            return this.value != null;
        }

        // Set the value of the attribute.
        public virtual void setValue(object value)
        {
            if (!this.isValid(value))
            {
                throw new TypeError(String.Format("{0} is not a valid value for %s", value.ToString(), this.name));
            }
            this.value = value;
        }

        // Return True if value is a valid value for Attribute.
        public virtual bool isValid(object value)
        {
            throw new NotImplementedException("Attribute.isValid() not implemented");
        }

        // Raise an exception if value is required and no value is present.
        public virtual void raiseIfRequired()
        {
            if (this.required && !this.hasValue())
            {
                throw new RequiredValueError(String.Format("A value is required for {0}", this.name));
            }
        }

        // Return the name as -name if it is not to be suppressed.
        public virtual object tclKey()
        {
            if (!this.suppressTclKey)
            {
                return String.Format(" -{0}", this.name);
            }
            else
            {
                return "";
            }
        }

        // Return the Tcl representation of the attribute name and value.
        public virtual string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            return String.Format("{0} {%s}", this.tclKey(), this.value);
        }
    }

    // A Constant is a constant value associated with an attribute name.
    public class Constant : Attribute
    {
        public Constant(object value)
            : base("", suppressTclKey: true)
        {
            this.value = value;
        }

        // Return the Tcl representation of the constant value.
        public override string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            return this.value as string;
        }
    }

    // A FloatAttribute is a float value associated with an attribute name.
    public class FloatAttribute
        : Attribute
    {
        int precision;
        public FloatAttribute(string name, int precision = 1)
            : base(name)
        {
            this.precision = precision;
        }

        // Return True if value is a float or int.
        public override bool isValid(object value)
        {
            return value is float || value is int;
        }

        // Return the Tcl representation of the attribute name and value.
        public override string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            var format = String.Format("{{0}} {{1:{0}F}}", this.precision);
            return String.Format(format, this.tclKey(), this.value);
        }
    }

    // An IntAttribute is an integer value associated with an
    //     attribute name.
    //     
    public class IntAttribute : Attribute
    {
        public IntAttribute(string name, string alias = null, bool required = false, bool suppressTclKey = false)
            : base(name, alias, required, suppressTclKey) { }
        // Return True if value is a valid value for an FloatAttribute.
        public override bool isValid(object value)
        {
            return value is int;
        }

        // Return the Tcl representation of the attribute name and value.
        public override string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            return String.Format("%s %d", this.tclKey(), this.value);
        }
    }

    // A DateAttribute is a datetime value associated with an
    //     attribute name.
    //     
    public class DateAttribute
        : Attribute
    {
        public DateAttribute(string name) : base(name) { }
        // Set the value only if one of datetime type is specified.
        public override void setValue(object value)
        {
            if (!(value is DateTime))
            {
                throw new TypeError(String.Format("%s is a %s, not a datetime type for %s", value.ToString(), value.GetType(), this.name));
            }
            this.value = value;
        }

        // Return True if the value is a datetime value.
        public override bool isValid(object value)
        {
            return value is DateTime;
        }

        // Return the Tcl representation of the attribute name and value.
        public override string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            var t = value as DateTime?;
            return String.Format("%s {%s}", this.tclKey(), t?.ToString("%m %d %H:%M"));
        }
    }

    // A StringAttribute is a string value associated with an
    //     attribute name.
    //     
    public class StringAttribute
        : Attribute
    {
        public StringAttribute(string msg, string alias = null, bool required = false, bool suppressTclKey = false)
            : base(msg, alias, required, suppressTclKey) { }
        // Return True if the value is a string.
        public override bool isValid(object value)
        {
            return value is string;
        }
    }

    // A WhenStringAttribute is a string value associated with an
    //     postscript command attribute name.  It can be one of
    //     "done", "error", or "always".
    //     
    public class WhenStringAttribute
        : StringAttribute
    {
        public WhenStringAttribute(string name) : base(name) { }
        static List<string> validList = new List<string>() { "done", "error", "always" };
        // Return True if the value is done, error, or always.
        public override bool isValid(object value)
        {
            return validList.Contains(value);
        }
    }

    // A StringListAttribute is a list of string values associated with an
    //     attribute name.
    //     
    public class StringListAttribute
        : Attribute
    {
        public StringListAttribute(string name, string alias = null, bool required = false, bool suppressTclKey = false)
            : base(name, alias, required, suppressTclKey) { }
        // Return True if the value is a list of strings.
        public override bool isValid(object value)
        {
            if (!(value is List<string>))
            {
                return false;
            }
            return true;
        }

        // Return True if there is at least one element in the list.
        public override bool hasValue()
        {
            var strList = this.value as List<string>;
            return strList != null && strList.Count > 0;
        }

        // Return the Tcl representation of the attribute name and value.
        public override string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            var args = new List<object>();
            foreach (var value in this.value as List<string>)
            {
                var val = value.ToString().Replace("\\", "\\\\");
                args.Add(String.Format("{%s}", val));
            }
            return String.Format("%s {%s}", this.tclKey(), string.Join(" ", args));
        }
    }

    // An IntListAttribute is a list of integer values associated with an
    //     attribute name.
    //     
    public class IntListAttribute
        : Attribute
    {
        public IntListAttribute(string name) : base(name) { }
        // Return True if the value is a list of integers.
        public override bool isValid(object value)
        {
            if (!(value is List<int>))
            {
                return false;
            }
            return true;
        }

        // Return True if there is at least one element in the list.
        public override bool hasValue()
        {
            return (this.value as List<string>).Count > 0;
        }

        // Return the Tcl representation of the attribute name and value.
        public override string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            return String.Format("%s {%s}", this.tclKey(), string.Join(" ", this.value as List<string>));
        }
    }

    // An ArgvAttribute is a list of string values associated with an
    //     attribute name.
    //     
    public class ArgvAttribute
        : StringListAttribute
    {
        public ArgvAttribute(string name, string alias = null, bool required = false, bool suppressTclKey = false)
            : base(name, alias, required, suppressTclKey) { }
        // Set the value, converting a string value to a list of strings.
        public override void setValue(object value)
        {
            if (value is string)
            {
                this.value = Indent.str2argv(value as string);
            }
            else
            {
                this.value = new List<string>() { ":" };
            }
        }
    }

    // A BooleanAttribute is a boolean value associated with an
    //     attribute name.
    //     
    public class BooleanAttribute : Attribute
    {
        public BooleanAttribute(string name) : base(name) { }
        // Return True if the value is 0 or 1.
        public override bool isValid(object value)
        {
            // values of True and False will pass as well
            return new List<object> { 0, 1 }.Contains(value);
        }

        // Return the Tcl representation of the attribute name and value.
        public override string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            return String.Format("%s %d", this.tclKey(), Convert.ToInt32(this.value));
        }
    }

    // A GroupAttribute is an attribute that contains multiple elements as a value
    //     (e.g. -init, -subtasks, -cmds), associated with an attribute name.
    //     
    public class GroupAttribute : Attribute
    {
        public GroupAttribute(string name, bool required = false)
            : base(name, required: required)
        {
            this.value = new List<Element>();
        }

        // Add the given element to the list of elements in this group.
        public virtual void addElement(Element element)
        {
            (this.value as List<Element>).Add(element);
        }

        // Return True if there is at least one element in the group.
        public override bool hasValue()
        {
            return (this.value as List<Element>).Count > 0;
        }

        // Return the index'th element of the group.
        public virtual object getItem(int index)
        {
            return (this.value as List<Element>)[index];
        }

        // Return the Tcl representation of the attribute name and value.
        public override string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            Indent.TclIndentLevel += 1;
            var lines = (from element in this.value as List<Element>
                         select (Indent.tclIndentStr() + element.asTcl())).ToList();
            Indent.TclIndentLevel -= 1;
            return String.Format(" -%s {\n%s\n%s}", this.name, string.Join("\n", lines), Indent.tclIndentStr());
        }
    }
    // An Element is a base class to represent components of a job that define
    //     the structure and content of a job.
    //     
    public abstract class Element
    {
        public object parent;
        public Element()
        {
            // keep track of parent to support instancing and to detect errors
            this.parent = null;
        }
        public virtual object getattr(string attr)
        {
            if (attr == "parent") return parent;
            return null;
        }
        public virtual void setattr(string attr, object value)
        {
            if (attr == "parent") parent = value;
            else throw new AttributeError(string.Format("%s is not a valid attribute of a %s", attr));
        }
        public abstract string asTcl();
    }

    // A KeyValueElement is an element that can have multiple attributes
    //     with associated values.  For example, a Job can have priority and
    //     title attributes.
    //     
    public class KeyValueElement : Element
    {
        public List<Attribute> attributes;
        public Dictionary<string, Attribute> attributeByName;
        public KeyValueElement(List<Attribute> attributes, Hashtable kw)
        {
            // lookup of attribute by name required for __getattr__ and __setattr__
            this.attributes = attributes;
            this.attributeByName = new Dictionary<string, Attribute>();
            foreach (var attr in attributes)
            {
                this.attributeByName[attr.name] = attr;
                if (attr.alias != null)
                {
                    this.attributeByName[attr.alias] = attr;
                }
            }
            // initialize attributes passes as keyword parameters
            foreach (string key in kw.Keys)
            {
                setattr(key, kw[key]);
            }
        }

        // Enable Attributes, which are specified in the self.attributes member,
        //         to be accessed as though they were members of the Element.
        //         
        public override object getattr(string attr)
        {
            if (this.attributeByName.ContainsKey(attr))
            {
                var attribute = this.attributeByName[attr];
                return attribute.value;
            }
            else
            {
                return base.getattr(attr);
            }
        }

        // Enable Attributes, which are specified in the self.attributes member,
        //         to be set as though they were members of the Element.  Attributes
        //         are restricted to those listed in self.attributes to avoid spelling
        //         mistakes from silently failing.  e.g. job.titlee = "A Title" will fail.
        //         
        public override void setattr(string attr, object value)
        {
            if (this.attributeByName.ContainsKey(attr))
            {
                var attribute = this.attributeByName[attr];
                attribute.setValue(value);
            }
            else
            {
                base.setattr(attr, value);
            }
        }

        // Return the Tcl representation of the Element's attribute
        //         names and values.
        //         
        public override string asTcl()
        {
            var parts = new List<object>();
            foreach (var attribute in this.attributes)
            {
                parts.Add(attribute.asTcl());
            }
            return string.Join("", parts);
        }
    }

    // A DirMap element defines a mapping between paths of two
    //     different OSes.
    //     
    public class DirMap : Element
    {
        string src;
        string dst;
        string zone;
        public DirMap(string src, string dst, string zone)
        {
            this.src = src;
            this.dst = dst;
            this.zone = zone;
        }
        // Return the Tcl representation of the dirmap expression.
        public override string asTcl()
        {
            return String.Format("{{{0}} {{1}} {2}}", this.src, this.dst, this.zone);
        }
    }

    // SubtaskMixin is a mix-in class for elements that can have child
    //     tasks, namely the Job, Task, and Iterate elements.
    //     
    public interface ISubtaskMixin { }
    public static class SubTaskMixinExtension
    {
        public static void addChild(this KeyValueElement self, Element element)
        {
            if (!(element is Task || element is Instance || element is Iterate))
            {
                throw new TypeError(String.Format("{0} is not an instance of Task, Instance, or Iterate", element.GetType().Name));
            }

            if ((element as Task)?.parent != null)
            {
                // this task already has a parent, so replace with an Instance
                var title = element.getattr("title") as string;
                var instance = new Instance(new Hashtable() { { "title", title } });
                (self.attributeByName["subtasks"] as GroupAttribute).addElement(instance);
            }
            else if (element.parent != null)
            {
                throw new ParentExistsError(String.Format("{0} is already a child of {1}", element.ToString(), element.parent.ToString()));
            }
            else
            {
                (self.attributeByName["subtasks"] as GroupAttribute).addElement(element);
            }
            element.parent = self;
        }

        // Instantiate a new Task element, add to subtask list, and return
        //         element.
        //         
        public static Task newTask(this KeyValueElement self, Hashtable kw, string argv = null)
        {
            var task = new Task(kw, argv);
            addChild(self, task);
            return task;
        }

        // Send script representing the job or task subtree, returning
        //         the job id of the new job.  Setting block to True will wait
        //         for the engine to submit the job before returning; in such a
        //         case, it's possible for an exception to be raised if the
        //         engine detects a syntax or logic error in the job.  A
        //         SpoolError exception is raised in the event of a communication
        //         error with the engine, or in the event the engine has a
        //         problem processing the job file (when blocked=True).
        //         The job's spoolfile and spoolhost attributes can be set
        //         with the coresponding keyword parameters; typically these
        //         are to show from which host a job has been spooled and
        //         the full path to the spooled job file.
        //         The engine can be targeted with the hostname port
        //         keyword parameters.
        //         
        // public virtual object spool(this ISubtaskMixin subTask,
        //     object block = false,
        //     object owner = null,
        //     object spoolfile = null,
        //     object spoolhost = null,
        //     object hostname = null,
        //     object port = null)
        // {
        //     // force the module engine client to set up a new connection.
        //     // EngineClient.close() doesn't work here because the spooler
        //     // is using EngineClient.spool(skipLogin=True), which causes
        //     // the EngineClient to reuse a cached TrHttpRPC connection.
        //     if (hostname || port)
        //     {
        //         ModuleEngineClient.conn = null;
        //         // prep engine client
        //         if (hostname)
        //         {
        //             ModuleEngineClient.setParam(hostname: hostname);
        //         }
        //         if (port)
        //         {
        //             ModuleEngineClient.setParam(port: port);
        //         }
        //     }
        //     // send spool message
        //     try
        //     {
        //         var result = ModuleEngineClient.spool(this.asTcl(), skipLogin: true, block: block, owner: owner, filename: spoolfile, hostname: spoolhost);
        //     }
        //     catch
        //     {
        //         throw SpoolError(String.Format("Spool error: %s", err.ToString()));
        //     }
        //     var resultDict = json.loads(result);
        //     return resultDict.get("jid");
        // }
    }

    // CleanupMixin is a mix-in class for elements that can have a cleanup
    //     attribute, namely the Job and Task elements.
    //     
    public interface ICleanupMixin { }
    public static class CleanupMixinExtension
    {
        // Instantiate a new Command element, adds to cleanup command
        //         list, and returns element.
        //         
        public static object newCleanup(this KeyValueElement item, Hashtable kw)
        {
            var command = new Command(kw);
            addCleanup(item, command);
            return command;
        }

        // Add an existing cleanup command to element.
        public static void addCleanup(this KeyValueElement item, Element command)
        {
            if (!(command is Command))
            {
                throw new TypeError(String.Format("{0} is not an instance of Command", command.ToString()));
            }
            var cleanup = item.attributeByName["cleanup"] as GroupAttribute;
            cleanup.addElement(command);
        }
    }

    // CleanupMixin is a mix-in class for elements that can have a cleanup
    //     attribute.  Currently this is only the Job element.
    //     
    public interface IPostscriptMixin { }
    public static class PostscriptMixinExtension
    {
        // Instantiate a new Command element, add to postscript command list,
        //         and return element.
        //         
        //         public static Command newPostscript(this Ipo params object[] kw)
        //         {
        //             var command = Command(kw);
        //             this.addPostscript(command);
        //             return command;
        //         }
        // 
        //         // Add an existing postscript command to element.
        //         public virtual object addPostscript(object command)
        //         {
        //             if (!(command is Command))
        //             {
        //                 throw TypeError(String.Format("%s is not an instance of Command", command.ToString()));
        //             }
        //             this.attributeByName["postscript"].addElement(command);
        //         }
    }

    // A Job element defines the attributes of a job and contains
    //     other elements definining the job, such as Tasks and directory
    //     mappings.
    //     
    public class Job : KeyValueElement, ISubtaskMixin, ICleanupMixin, IPostscriptMixin
    {
        public Job(Hashtable kw) : base(Attributes(), kw)
        {
        }
        static List<Attribute> Attributes()
        {
            return new List<Attribute> {
                    new Constant("Job"),
                    new StringAttribute("title", required: true),
                    new StringAttribute("tier"),
                    new StringAttribute("spoolcwd"),
                    new StringListAttribute("projects"),
                    new StringListAttribute("crews"),
                    new IntAttribute("maxactive"),
                    new BooleanAttribute("paused"),
                    new DateAttribute("after"),
                    new IntListAttribute("afterjids"),
                    new GroupAttribute("init"),
                    new IntAttribute("atleast"),
                    new IntAttribute("atmost"),
                    new IntAttribute("etalevel"),
                    new StringListAttribute("tags"),
                    new FloatAttribute("priority"),
                    new StringAttribute("service"),
                    new StringListAttribute("envkey"),
                    new StringAttribute("comment"),
                    new StringAttribute("metadata"),
                    new StringAttribute("editpolicy"),
                    new GroupAttribute("cleanup"),
                    new GroupAttribute("postscript"),
                    new GroupAttribute("dirmaps"),
                    new BooleanAttribute("serialsubtasks"),
                    new GroupAttribute("subtasks", required: true)
                };
        }
        // Instantiates a new DirMap element, add to job's dirmap list, and
        //         returns element.
        //         
        public virtual DirMap newDirMap(string src, string dst, string zone)
        {
            var dirmap = new DirMap(src, dst, zone);
            var dirmaps = this.attributeByName["dirmaps"] as GroupAttribute;
            dirmaps.addElement(dirmap);
            return dirmap;
        }

        public override string ToString()
        {
            string title = this.getattr("title") as string;
            if (title == null) title = "<no title>";
            return String.Format("Job {0}", title);
        }
    }

    // A Task element defines the attributes of a task and contains
    //     other elements definining the task such as commands and subtasks.
    public class Task : KeyValueElement, ISubtaskMixin, ICleanupMixin
    {
        public Task(Hashtable kw, string argv = null) : base(Attributes(), kw)
        {
            if (argv != null)
            {
                var command = new Command(new Hashtable() { { "argv", argv } });
                this.addCommand(command);
            }
        }

        public static List<Attribute> Attributes()
        {
            return new List<Attribute> {
                new Constant("Task"),
                new StringAttribute("title", required: true, suppressTclKey: true),
                new StringAttribute("id"),
                new StringAttribute("service"),
                new IntAttribute("atleast"),
                new IntAttribute("atmost"),
                new GroupAttribute("cmds"),
                new ArgvAttribute("chaser"),
                new ArgvAttribute("preview"),
                new BooleanAttribute("serialsubtasks"),
                new BooleanAttribute("resumeblock"),
                new GroupAttribute("cleanup"),
                new StringAttribute("metadata"),
                new GroupAttribute("subtasks")
            };
        }

        // Add the specified Command to command list of the Task.
        public virtual void addCommand(Command command)
        {
            var cmds = this.attributeByName["cmds"] as GroupAttribute;
            cmds.addElement(command);
        }

        // Instantiate a new Command element, add to command list, and return
        //         element.
        //         
        public virtual Command newCommand(Hashtable kw)
        {
            var command = new Command(kw);
            this.addCommand(command);
            return command;
        }

        public override string ToString()
        {
            string title = this.getattr("title") as string;
            if (title == null) title = "<no title>";
            return String.Format("Task {0}", title);
        }
    }

    // An Instance is an element whose state is tied to that of another
    //     task.
    //     
    public class Instance : KeyValueElement
    {
        public Instance(Hashtable kw) : base(Attributes(), kw)
        {
        }

        public static List<Attribute> Attributes()
        {
            return new List<Attribute> {
                    new Constant("Instance"),
                    new StringAttribute("title", required: true, suppressTclKey: true)
                };
        }

        public override string ToString()
        {
            string title = this.getattr("title") as string;
            if (title == null) title = "<no title>";
            return String.Format("Instance {0}", title);
        }
    }

    // An Iterate element defines a corresponding iteration loop.
    public class Iterate : KeyValueElement, ISubtaskMixin
    {
        public static List<Attribute> Attributes()
        {
            return new List<Attribute> {
                    new Constant("Iterate"),
                    new StringAttribute("varname", required: true, suppressTclKey: true),
                    new IntAttribute("from", alias: "frm", required: true),
                    new IntAttribute("to", required: true),
                    new IntAttribute("by"),
                    new GroupAttribute("template", required: true),
                    new GroupAttribute("subtasks")
                };
        }
        public Iterate(Hashtable kw) : base(Attributes(), kw)
        {
        }

        // Add the specified task to the Iterate template.
        public virtual void addToTemplate(Element task)
        {
            if (!(task is Task || task is Instance || task is Iterate))
            {
                throw new TypeError(String.Format("{0} is not an instance of Task, Instance, or Iterate", task.GetType()));
            }
            var template = this.attributeByName["template"] as GroupAttribute;
            template.addElement(task);
        }

        public override string ToString()
        {
            string varname = this.getattr("varname") as string;
            if (varname == null) varname = "<no iterator>";
            return String.Format("Iterate {0}", varname);
        }
    }

    // A Command element defines the attributes of a command.
    public class Command : KeyValueElement
    {
        public Command(Hashtable kw, bool local = false) : base(Attributes(local), kw)
        {
        }

        public static List<Attribute> Attributes(bool local)
        {
            object cmdtype;
            if (local)
            {
                cmdtype = "Command";
            }
            else
            {
                cmdtype = "RemoteCmd";
            }
            return new List<Attribute> {
                    new Constant(cmdtype),
                    new ArgvAttribute("argv", required: true, suppressTclKey: true),
                    new StringAttribute("msg"),
                    new StringListAttribute("tags"),
                    new StringAttribute("service"),
                    new StringAttribute("metrics"),
                    new StringAttribute("id"),
                    new StringAttribute("refersto"),
                    new BooleanAttribute("expand"),
                    new IntAttribute("atleast"),
                    new IntAttribute("atmost"),
                    new IntAttribute("minrunsecs"),
                    new IntAttribute("maxrunsecs"),
                    new BooleanAttribute("samehost"),
                    new StringListAttribute("envkey"),
                    new IntListAttribute("retryrc"),
                    new WhenStringAttribute("when"),
                    new StringListAttribute("resumewhile"),
                    new BooleanAttribute("resumepin"),
                    new StringAttribute("metadata")
                };
        }
    }
}
