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
    public class RequiredValueError : AuthorError
    {
        public RequiredValueError(string msg) : base(msg) { }
    }

    // Raised when attempting to add a task to multiple parent tasks.
    public class ParentExistsError : AuthorError
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
        public static List<string> str2argv(string s)
        {
            return s.Split().ToList();
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
            if(!this.required && value == null)
            {
                this.value = null;
                return;
            }
            if (!this.isValid(value))
            {
                throw new TypeError(String.Format("{0} is not a valid value for {1}", value.ToString(), this.name));
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
            return String.Format("{0} {{{1}}}", this.tclKey(), this.value);
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
    public class FloatAttribute : Attribute
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
            var valueStr = string.Format($"{{0:F{this.precision}}}", this.value);
            return $"{this.tclKey()} {{{valueStr}}}";
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
            return String.Format("{0} {1}", this.tclKey(), this.value);
        }
    }

    // A DateAttribute is a datetime value associated with an
    //     attribute name.
    //     
    public class DateAttribute : Attribute
    {
        public DateAttribute(string name) : base(name) { }
        // Set the value only if one of datetime type is specified.
        public override void setValue(object value)
        {
            if (!(value is DateTime))
            {
                throw new TypeError(String.Format("{0} is a {1}, not a datetime type for {2}", value.ToString(), value.GetType(), this.name));
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
            return String.Format("{0} {{{1}}}", this.tclKey(), t?.ToString("%M %d %H:%m"));
        }
    }

    // A StringAttribute is a string value associated with an
    //     attribute name.
    //     
    public class StringAttribute : Attribute
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
    public class WhenStringAttribute : StringAttribute
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
    public class StringListAttribute : Attribute
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
            var strList = this.value as ICollection<string>;
            return strList?.Count > 0;
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
            foreach (var value in this.value as ICollection<string>)
            {
                var val = value.ToString().Replace("\\", "\\\\");
                args.Add(String.Format("{{{0}}}", val));
            }
            return String.Format("{0} {{{1}}}", this.tclKey(), string.Join(" ", args));
        }
    }

    // An IntListAttribute is a list of integer values associated with an
    //     attribute name.
    //     
    public class IntListAttribute : Attribute
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
            var intList = this.value as List<int>;
            return intList?.Count > 0;
        }

        // Return the Tcl representation of the attribute name and value.
        public override string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            return String.Format("{0} {{{1}}}", this.tclKey(), string.Join(" ", this.value as IEnumerable<int>));
        }
    }

    // An ArgvAttribute is a list of string values associated with an
    //     attribute name.
    //     
    public class ArgvAttribute : StringListAttribute
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
            else if ((value as ICollection<string>)?.Count > 0)
            {
                this.value = value;
            }
            else
            {
                throw new AttributeError(string.Format("argv value can not accept {0}", value));
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
            return new List<object> { true, false }.Contains(value);
        }

        // Return the Tcl representation of the attribute name and value.
        public override string asTcl()
        {
            this.raiseIfRequired();
            if (!this.hasValue())
            {
                return "";
            }
            return String.Format("{0} {1}", this.tclKey(), Convert.ToInt32(this.value));
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
            return String.Format(" -{0} {{\n{1}\n{2}}}", this.name, string.Join("\n", lines), Indent.tclIndentStr());
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
            else throw new AttributeError(string.Format("{0} is not a valid attribute of a {1}", attr, this.GetType().Name));
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
        public KeyValueElement(List<Attribute> attributes)
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
            return String.Format("{{{0} {1} {2}}}", $"{{{this.src}}}", $"{{{this.dst}}}", $"{this.zone}");
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
                var instance = new Instance(title);
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
        public static Task newTask(this KeyValueElement self
            , string title = null
            , string service = null
            , string argv = null)
        {
            var task = new Task(title:title, argv:argv, service: service);
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
        public static object newCleanup(this KeyValueElement item, object argv = null)
        {
            var command = new Command(argv);
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
        public static Command newPostscript(this KeyValueElement self, string service = null, string when = null, object argv = null)
        {
            var command = new Command(argv: argv) { service = service, when = when };
            if (service != null) command.service = service;
            if (when != null) command.when = when;
            addPostscript(self, command);
            return command;
        }

        // Add an existing postscript command to element.
        public static void addPostscript(this KeyValueElement self, Command command)
        {
            if (!(command is Command))
            {
                throw new TypeError(String.Format("{0} is not an instance of Command", command.ToString()));
            }
            var posts = self.attributeByName["postscript"] as GroupAttribute;
            posts.addElement(command);
        }
    }

    // A Job element defines the attributes of a job and contains
    //     other elements definining the job, such as Tasks and directory
    //     mappings.
    //     
    public class Job : KeyValueElement, ISubtaskMixin, ICleanupMixin, IPostscriptMixin
    {
        public string title
        {
            get { return this.getattr("title") as string; }
            set { this.setattr("title", value); }
        }
        public string tier
        {
            get { return this.getattr("tier") as string; }
            set { this.setattr("tier", value); }
        }
        public string spoolcwd
        {
            get { return this.getattr("spoolcwd") as string; }
            set { this.setattr("spoolcwd", value); }
        }
        public List<string> projects
        {
            get { return this.getattr("projects") as List<string>; }
            set { this.setattr("projects", value); }
        }
        public List<string> crews
        {
            get { return this.getattr("crews") as List<string>; }
            set { this.setattr("crews", value); }
        }
        public int? maxactive
        {
            get { return this.getattr("maxactive") as int?; }
            set { this.setattr("maxactive", value); }
        }
        public bool paused
        {
            get { return true == this.getattr("paused") as bool?; }
            set { this.setattr("paused", value); }
        }
        public DateTime? after
        {
            get { return this.getattr("after") as DateTime?; }
            set { this.setattr("after", value); }
        }
        public List<int> afterjids
        {
            get { return this.getattr("afterjids") as List<int>; }
            set { this.setattr("afterjids", value); }
        }
        public List<object> init
        {
            get { return this.getattr("init") as List<object>; }
            set { this.setattr("init", value); }
        }
        public int? atleast
        {
            get { return this.getattr("atleast") as int?; }
            set { this.setattr("atleast", value); }
        }
        public int? atmost
        {
            get { return this.getattr("atmost") as int?; }
            set { this.setattr("atmost", value); }
        }
        public int? etalevel
        {
            get { return this.getattr("etalevel") as int?; }
            set { this.setattr("etalevel", value); }
        }
        public List<string> tags
        {
            get { return this.getattr("tags") as List<string>; }
            set { this.setattr("tags", value); }
        }
        public float? priority
        {
            get { return this.getattr("priority") as float?; }
            set { this.setattr("priority", value); }
        }
        public string service
        {
            get { return this.getattr("service") as string; }
            set { this.setattr("service", value); }
        }
        public List<string> envkey
        {
            get { return this.getattr("envkey") as List<string>; }
            set { this.setattr("envkey", value); }
        }
        public string comment
        {
            get { return this.getattr("comment") as string; }
            set { this.setattr("comment", value); }
        }
        public string metadata
        {
            get { return this.getattr("metadata") as string; }
            set { this.setattr("metadata", value); }
        }
        public string editpolicy
        {
            get { return this.getattr("editpolicy") as string; }
            set { this.setattr("editpolicy", value); }
        }
        public List<Element> cleanup
        {
            get { return this.getattr("cleanup") as List<Element>; }
            set { this.setattr("cleanup", value); }
        }
        public List<Element> postscript
        {
            get { return this.getattr("postscript") as List<Element>; }
            set { this.setattr("postscript", value); }
        }
        public List<Element> dirmaps
        {
            get { return this.getattr("dirmaps") as List<Element>; }
            set { this.setattr("dirmaps", value); }
        }
        public bool serialsubtasks
        {
            get { return true == this.getattr("serialsubtasks") as bool?; }
            set { this.setattr("serialsubtasks", value); }
        }
        public List<Element> subtasks
        {
            get { return this.getattr("subtasks") as List<Element>; }
            set { this.setattr("subtasks", value); }
        }

        public Job(string title = null) : base(Attributes())
        {
            if (title != null) this.title = title;
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
    //     other elements defining the task such as commands and subtasks.
    public class Task : KeyValueElement, ISubtaskMixin, ICleanupMixin
    {
        public Task(string title = null, object argv = null, string service = null) : base(Attributes())
        {
            if (title != null)
            {
                this.title = title;
            }
            if (argv == null && service != null)
            {
                this.service = service;
            }
            else if (argv != null)
            {
                var command = new Command(argv: argv) { service = service };
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

        public string title
        {
            get { return this.getattr("title") as string; }
            set { this.setattr("title", value); }
        }
        public string id
        {
            get { return this.getattr("id") as string; }
            set { this.setattr("id", value); }
        }
        public string service
        {
            get { return this.getattr("service") as string; }
            set { this.setattr("service", value); }
        }
        public int? atleast
        {
            get { return this.getattr("atleast") as int?; }
            set { this.setattr("atleast", value); }
        }
        public int? atmost
        {
            get { return this.getattr("atmost") as int?; }
            set { this.setattr("atmost", value); }
        }
        public List<Element> cmds
        {
            get { return this.getattr("cmds") as List<Element>; }
            set { this.setattr("cmds", value); }
        }
        public object chaser
        {
            get { return this.getattr("chaser") as object; }
            set { this.setattr("chaser", value); }
        }
        public object preview
        {
            get { return this.getattr("preview") as object; }
            set { this.setattr("preview", value); }
        }
        public bool serialsubtasks
        {
            get { return true == this.getattr("serialsubtasks") as bool?; }
            set { this.setattr("serialsubtasks", value); }
        }
        public bool resumeblock
        {
            get { return true == this.getattr("resumeblock") as bool?; }
            set { this.setattr("resumeblock", value); }
        }
        public List<Element> cleanup
        {
            get { return this.getattr("cleanup") as List<Element>; }
            set { this.setattr("cleanup", value); }
        }
        public string metadata
        {
            get { return this.getattr("metadata") as string; }
            set { this.setattr("metadata", value); }
        }
        public List<Element> subtasks
        {
            get { return this.getattr("subtasks") as List<Element>; }
            set { this.setattr("subtasks", value); }
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
        public virtual Command newCommand(Dictionary<string, object> kw)
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
        public Instance(string title = null) : base(Attributes())
        {
            if (title != null) this.title = title;
        }
        public static List<Attribute> Attributes()
        {
            return new List<Attribute> {
                    new Constant("Instance"),
                    new StringAttribute("title", required: true, suppressTclKey: true)
                };
        }
        public string title
        {
            get { return this.getattr("title") as string; }
            set { this.setattr("title", value); }
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
        public string varname
        {
            get { return this.getattr("varname") as string; }
            set { this.setattr("varname", value); }
        }
        public int from
        {
            get { return (int)this.getattr("from"); }
            set { this.setattr("from", value); }
        }
        public int frm
        {
            get { return (int)this.getattr("from"); }
            set { this.setattr("from", value); }
        }
        public int to
        {
            get { return (int)this.getattr("to"); }
            set { this.setattr("to", value); }
        }
        public int by
        {
            get { return (int)this.getattr("by"); }
            set { this.setattr("by", value); }
        }
        public List<Element> template
        {
            get { return this.getattr("template") as List<Element>; }
            set { this.setattr("template", value); }
        }
        public List<Element> subtasks
        {
            get { return this.getattr("subtasks") as List<Element>; }
            set { this.setattr("subtasks", value); }
        }

        public Iterate() : base(Attributes())
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
        public Command(object argv = null, bool local = false) : base(Attributes(local))
        {
            if(argv!=null)this.argv = argv;
        }
        public static List<Attribute> Attributes(bool local)
        {
            string cmdtype = local ? "Command" : "RemoteCmd";
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

        public object argv
        {
            get { return this.getattr("argv"); }
            set { this.setattr("argv", value); }
        }
        public string msg
        {
            get { return this.getattr("msg") as string; }
            set { this.setattr("msg", value); }
        }
        public List<string> tags
        {
            get { return this.getattr("tags") as List<string>; }
            set { this.setattr("tags", value); }
        }
        public string service
        {
            get { return this.getattr("service") as string; }
            set { this.setattr("service", value); }
        }
        public string metrics
        {
            get { return this.getattr("metrics") as string; }
            set { this.setattr("metrics", value); }
        }
        public string id
        {
            get { return this.getattr("id") as string; }
            set { this.setattr("id", value); }
        }
        public string refersto
        {
            get { return this.getattr("refersto") as string; }
            set { this.setattr("refersto", value); }
        }
        public bool expand
        {
            get { return this.getattr("expand") as bool? ?? false; }
            set { this.setattr("expand", value); }
        }
        public int atleast
        {
            get { return this.getattr("atleast") as int? ?? 0; }
            set { this.setattr("atleast", value); }
        }
        public int atmost
        {
            get { return this.getattr("atmost") as int? ?? 0; }
            set { this.setattr("atmost", value); }
        }
        public int minrunsecs
        {
            get { return this.getattr("minrunsecs") as int? ?? 0; }
            set { this.setattr("minrunsecs", value); }
        }
        public int maxrunsecs
        {
            get { return this.getattr("maxrunsecs") as int? ?? 0; }
            set { this.setattr("maxrunsecs", value); }
        }
        public bool samehost
        {
            get { return this.getattr("samehost") as bool? ?? false; }
            set { this.setattr("samehost", value); }
        }
        public List<string> envkey
        {
            get { return this.getattr("envkey") as List<string>; }
            set { this.setattr("envkey", value); }
        }
        public List<int> retryrc
        {
            get { return this.getattr("retryrc") as List<int>; }
            set { this.setattr("retryrc", value); }
        }
        public string when
        {
            get { return this.getattr("when") as string; }
            set { this.setattr("when", value); }
        }
        public List<string> resumewhile
        {
            get { return this.getattr("resumewhile") as List<string>; }
            set { this.setattr("resumewhile", value); }
        }
        public bool resumepin
        {
            get { return this.getattr("resumepin") as bool? ?? false; }
            set { this.setattr("resumepin", value); }
        }
        public string metadata
        {
            get { return this.getattr("metadata") as string; }
            set { this.setattr("metadata", value); }
        }

    }
}
