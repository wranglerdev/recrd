class Recrd < Formula
  desc "Automated E2E test recorder for Robot Framework"
  homepage "https://github.com/recrd/recrd"
  url "https://github.com/recrd/recrd/releases/download/v{{VERSION}}/recrd-{{VERSION}}-osx-x64.tar.gz"
  sha256 "{{SHA256}}"
  license "MIT"

  # We support Intel and ARM macOS via multi-arch binary or separate formulae.
  # For simplicity, we default to x64 but handle the ARM case if needed.
  if Hardware::CPU.arm?
    url "https://github.com/recrd/recrd/releases/download/v{{VERSION}}/recrd-{{VERSION}}-osx-arm64.tar.gz"
    sha256 "{{SHA256_ARM}}"
  end

  def install
    # Install binary and playwright driver to libexec
    libexec.install "recrd", ".playwright"
    
    # Symlink the binary to bin/recrd
    (bin/"recrd").make_relative_symlink(libexec/"recrd")
  end

  test do
    # Basic check that the binary runs and returns the version
    system "#{bin}/recrd", "--version"
  end
end
