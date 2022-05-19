class LoadError < Exception
end

$stdout = MRubyUnity::Console.new

module Kernel
  def puts(*args)
    $stdout.write args.join("\n")
  end

  def print(*args)
    $stdout.write args.join("\n")
  end
end

module Kernel
  def p(*args)
    args.each { |x| puts x.inspect }
  end
end
