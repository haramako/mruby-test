TOPLEVEL_BINDING = binding
$LOAD_PATH = ["."]

class LoadError < Exception
end

module MRubyUnity
  class ModuleLoader
    def self.instance
      @instance ||= ModuleLoader.new
      @instance
    end

    def require(name, original_caller)
      if name.start_with?("./")
        require_relative name, original_caller
      else
        $LOAD_PATH.each do |dir|
          return if require_if_exist("#{dir}/#{name}")
        end
        raise LoadError.new("cannot load such file -- #{name}")
      end
    end

    def require_relative(name, original_caller)
      basepath = original_caller[0].split(":")[0]
      basedir = File.dirname(basepath)
      basedir = "." if basedir.empty?
      name = name[2..-1] if name.start_with?("./")
      unless require_if_exist("#{basedir}/#{name}")
        raise LoadError.new("cannot load such file -- #{name}")
      end
    end

    def initialize
      @required = {}
    end

    def require_if_exist(path)
      path += ".rb" unless path.end_with?(".rb")
      return false unless File.exist?(path)
      unless @required[path]
        src = IO.read(path)
        eval(src, TOPLEVEL_BINDING, path)
        @required[path] = true
      end
      true
    end
  end

  class DummyIO < IO
    def initialize(f)
      @f = f
    end

    def write(str)
      @f.write str.to_s
    end
  end
end

module Kernel
  def require(name)
    MRubyUnity::ModuleLoader.instance.require(name, caller)
  end

  def require_relative(name)
    MRubyUnity::ModuleLoader.instance.require_relative(name, caller)
  end
end

module Kernel
  def p(*args)
    args.each { |v| puts v.inspect }
  end
end

$stdout = MRubyUnity::DummyIO.new($stdout)

puts "start"
require "test/require_test"

require_relative("./test/require_test.rb")
